namespace Chess
{
	using Sandbox;
	using System.Collections.Generic;
	using System.Linq;

	[Library( "ent_chesspiece", Title = "Chess Piece", Spawnable = true )]

	public partial class ChessPiece : Prop
	{
		[Net]
		public int PieceType { get; set; }
		[Net]
		public bool FirstMove { get; set; } = true;
		string[] pieceTypes = { "models/chess_pieces/pawn.vmdl", "models/chess_pieces/rook.vmdl", "models/chess_pieces/horse.vmdl", "models/chess_pieces/bishop.vmdl", "models/chess_pieces/queen.vmdl", "models/chess_pieces/king.vmdl" };
		[Net]
		public int UpInt { get; set; }
		[Net]
		public int SideInt { get; set; }
		[Net]
		public int Team { get; set; } = 1;


		private int ConcatInt(int a, int b ) // Yeah very nice
		{
			return int.Parse( a.ToString() + b.ToString() );
		}

		public override void Spawn()
		{
			base.Spawn();

			SetModel( "models/chess_pieces/rook.vmdl" );
			Scale = 1f;
			RenderColor = Color.White;
			Rotation = Rotation.From( 0f, -90f, 0f );

			SetupPhysicsFromModel( PhysicsMotionType.Static, false );
		}

		public ChessPiece InDanger()
		{
			ChessPiece result = null;

			foreach (var piece in Entity.All.OfType<ChessPiece>() )
			{
				if ( piece.Team == Team )
					continue;

				var moves = piece.GetMoves();

				foreach (var move in moves )
				{
					string num_str = move.ToString();
					int up = (int)(num_str[0]) - 48;
					int side = (int)(num_str[1]) - 48;

					if (up == UpInt && side == SideInt )
					{
						result = piece;
						break;
					}
				}
			}

			return result;
		}

		public bool CanSaveKing()
		{
			bool result = false;
			var game = ChessGame.Current;
			var king = (Team == 1 ? game.white_player : game.black_player)?.King;
			var danger = king?.InDanger();
			var moves = GetMoves();

			if ( danger.IsValid() )
			{
				foreach ( var move in moves )
				{
					string num_str = move.ToString();
					int up = (int)(num_str[0]) - 48;
					int side = (int)(num_str[1]) - 48;

					if ( up == danger.UpInt && side == danger.SideInt )
					{
						result = true;
						break;
					}
				}
			} else { result = true;  }

			return result;
		}

		//public List<int> CanShieldKingMoves() // TODO: Make helper functions to make this a clean solution...
		//{
		//	var game = ChessGame.Current;
		//	var king = (Team == 1 ? game.white_player : game.black_player)?.King;
		//	var danger = king?.InDanger();

		//	var blockers = new Dictionary<int, bool>(); ;

		//	if ( danger.IsValid() )
		//	{
		//		var 
		//		var moves = danger.GetMoves(false, moves );

		//	}

		//	return blockers;
		//}

		public void SetBlack()
		{
			RenderColor = new Color( .2f, .2f, .2f );
			Rotation = Rotation.From( 0f, 90f, 0f );
			Team = 2;
		}

		public async void SetCell(int up, int side)
		{			
			UpInt = up;
			SideInt = side;

			Position = ChessGame.Current.GetPiecePosition( up, side );

			await Task.Delay(30);
			Sound.FromEntity( "chess_move", this );
		}

		public bool CanMove(int wanted_up, int wanted_side )
		{
			bool canmove = false;

			var moves = GetMoves();

			foreach ( var result in moves )
			{
				string num_str = result.ToString();
				int up = (int)(num_str[0]) - 48;
				int side = (int)(num_str[1]) - 48;

				if ( up > 8 || up < 1 || side > 8 || side < 1 )
					continue;

				if ( up == wanted_up && side == wanted_side )
				{
					canmove = true;
					break;
				}
			}

			return canmove;
		}

		public bool InBounds(int up, int side)
		{
			return (up > 0 && up < 9 && side > 0 && side < 9);
		}

		public bool IsFriendlyBlocked(int up, int side, bool forceisvalid = false )
		{
			ChessPiece ent = ChessGame.Current.GetOccupant( up, side );

			return (ent.IsValid() ? ent.Team == Team : (forceisvalid ? true : false));
		}

		public bool HandleAfterMove()
		{
			if ( PieceType == 1 )
			{
				bool IsBlack = Team == 2;
				if (UpInt == (IsBlack ? 8 : 1) )
				{
					Log.Info(UpInt);
					var game = ChessGame.Current;
					var ply = IsBlack ? game.black_player : game.white_player;
					ply.PromotingPiece = this;

					ply.SetPromotionScreen( true );

					return false;
				} else
				{
					return true;
				}
			}

			return true;
		}
		
		public List<int> GetSafeMoves( bool mark = false)
		{
			var moves = GetMoves();

			var badMoves = new Dictionary<int, bool>(); ;
			List<int> goodMoves = new List<int>();

			foreach ( var ent in Entity.All.OfType<ChessPiece>())
			{
				if ( ent.Team == Team )
					continue;

				var ent_moves = ent.GetMoves();

				foreach (var move in ent_moves )
				{
					badMoves[move] = true;
				}
			}

			foreach ( var move in moves )
			{
				if ( !badMoves.ContainsKey(move) )
				{
					goodMoves.Add( move );

					if ( IsClient && mark )
					{
						string num_str = move.ToString();
						int up = (int)(num_str[0]) - 48;
						int side = (int)(num_str[1]) - 48;
						ChessGame.Current.SetMarkedCell( up, side, true, ChessGame.Current.GetOccupant( up, side ).IsValid() );
					}
				}
			}

			return goodMoves;
		}

		public List<int> GetMoves( bool mark = false, List<int> blockers = null ) // TODO: Clean this up
		{
			List<int> moves = new List<int>();

			bool IsBlack = Team == 2;
			if ( PieceType == 1 ) // Pawn
			{
				if ( !ChessGame.Current.IsCellOccupied( UpInt + (IsBlack ? 1 : -1), SideInt ) )
				{
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt ) );

					if ( FirstMove && !ChessGame.Current.IsCellOccupied( UpInt + (IsBlack ? 2 : -2), SideInt ) )
					{
						moves.Add( ConcatInt( UpInt + (IsBlack ? 2 : -2), SideInt ) );
					}
				}

				if ( !IsFriendlyBlocked( UpInt + (IsBlack ? 1 : -1), SideInt - 1, true ) )
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt - 1 ) );

				if ( !IsFriendlyBlocked( UpInt + (IsBlack ? 1 : -1), SideInt + 1, true ) )
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt + 1 ) );
			}
			else if ( PieceType == 2 ) // Rook
			{
				bool upblocked = false;
				bool downblocked = false;
				bool leftblocked = false;
				bool rightblocked = false;

				bool addedTargetup = false;
				bool addedTargetdown = false;
				bool addedTargetleft = false;
				bool addedTargetright = false;

				for ( int i = 1; i < 8; i++ )
				{
					var nextUp = UpInt + i;
					var nextDown = UpInt - i;
					var nextRight = SideInt + i;
					var nextLeft = SideInt - i;

					if ( InBounds( nextUp, SideInt ) && !upblocked )
						if ( IsFriendlyBlocked( nextUp, SideInt ) )
							upblocked = true;
						else if ( !addedTargetup )
						{
							if ( !IsFriendlyBlocked( nextUp, SideInt, true ) )
								addedTargetup = true;

							moves.Add( ConcatInt( nextUp, SideInt ) );
						}

					if ( InBounds( nextDown, SideInt ) && !downblocked )
						if ( IsFriendlyBlocked( nextDown, SideInt ) )
							downblocked = true;
						else if ( !addedTargetdown )
						{
							if ( !IsFriendlyBlocked( nextDown, SideInt, true ) )
								addedTargetdown = true;

							moves.Add( ConcatInt( nextDown, SideInt ) );
						}

					if ( InBounds( UpInt, nextLeft ) && !leftblocked )
						if ( IsFriendlyBlocked( UpInt, nextLeft ) )
							leftblocked = true;
						else if ( !addedTargetleft )
						{
							if ( !IsFriendlyBlocked( UpInt, nextLeft, true ) )
								addedTargetleft = true;

							moves.Add( ConcatInt( UpInt, nextLeft ) );
						}



					if ( InBounds( UpInt, nextRight ) && !rightblocked )
						if ( IsFriendlyBlocked( UpInt, nextRight ) )
							rightblocked = true;
						else if ( !addedTargetright )
						{
							if ( !IsFriendlyBlocked( UpInt, nextRight, true ) )
								addedTargetright = true;

							moves.Add( ConcatInt( UpInt, nextRight ) );
						}
				}
			}
			else if ( PieceType == 3 ) // Horse
			{

				if ( InBounds( UpInt + 2, SideInt - 1 ) && !IsFriendlyBlocked( UpInt + 2, SideInt - 1 ) )
					moves.Add( ConcatInt( UpInt + 2, SideInt - 1 ) );

				if ( InBounds( UpInt + 2, SideInt + 1 ) && !IsFriendlyBlocked( UpInt + 2, SideInt + 1 ) )
					moves.Add( ConcatInt( UpInt + 2, SideInt + 1 ) );

				if ( InBounds( UpInt - 2, SideInt - 1 ) && !IsFriendlyBlocked( UpInt - 2, SideInt - 1 ) )
					moves.Add( ConcatInt( UpInt - 2, SideInt - 1 ) );

				if ( InBounds( UpInt - 2, SideInt + 1 ) && !IsFriendlyBlocked( UpInt - 2, SideInt + 1 ) )
					moves.Add( ConcatInt( UpInt - 2, SideInt + 1 ) );

				if ( InBounds( UpInt + 1, SideInt - 2 ) && !IsFriendlyBlocked( UpInt + 1, SideInt - 2 ) )
					moves.Add( ConcatInt( UpInt + 1, SideInt - 2 ) );

				if ( InBounds( UpInt - 1, SideInt - 2 ) && !IsFriendlyBlocked( UpInt - 1, SideInt - 2 ) )
					moves.Add( ConcatInt( UpInt - 1, SideInt - 2 ) );

				if ( InBounds( UpInt + 1, SideInt + 2 ) && !IsFriendlyBlocked( UpInt + 1, SideInt + 2 ) )
					moves.Add( ConcatInt( UpInt + 1, SideInt + 2 ) );

				if ( InBounds( UpInt - 1, SideInt + 2 ) && !IsFriendlyBlocked( UpInt - 1, SideInt + 2 ) )
					moves.Add( ConcatInt( UpInt - 1, SideInt + 2 ) );
			}
			else if ( PieceType == 4 ) // Bishop
			{
				bool leftup_blocked = false;
				bool righttup_blocked = false;
				bool leftdown_blocked = false;
				bool rightdown_blocked = false;

				bool addedtarget_leftup = false;
				bool addedtarget_rightup = false;
				bool addedtarget_leftdown = false;
				bool addedtarget_rightdown = false;

				for ( int i = 1; i < 8; i++ )
				{
					if ( InBounds( UpInt + i, SideInt - i ) && !leftup_blocked )
						if ( IsFriendlyBlocked( UpInt + i, SideInt - i ) )
							leftup_blocked = true;
						else if ( !addedtarget_leftup )
						{
							if ( !IsFriendlyBlocked( UpInt + i, SideInt - i, true ) )
								addedtarget_leftup = true;

							moves.Add( ConcatInt( UpInt + i, SideInt - i ) );
						}

					if ( InBounds( UpInt + i, SideInt + i ) && !righttup_blocked )
						if ( IsFriendlyBlocked( UpInt + i, SideInt + i ) )
							righttup_blocked = true;
						else if ( !addedtarget_rightup )
						{
							if ( !IsFriendlyBlocked( UpInt + i, SideInt + i, true ) )
								addedtarget_rightup = true;

							moves.Add( ConcatInt( UpInt + i, SideInt + i ) );
						}

					if ( InBounds( UpInt - i, SideInt - i ) && !leftdown_blocked )
						if ( IsFriendlyBlocked( UpInt - i, SideInt - i ) )
							leftdown_blocked = true;
						else if ( !addedtarget_leftdown )
						{
							if ( !IsFriendlyBlocked( UpInt - i, SideInt - i, true ) )
								addedtarget_leftdown = true;

							moves.Add( ConcatInt( UpInt - i, SideInt - i ) );
						}

					if ( InBounds( UpInt - i, SideInt + i ) && !rightdown_blocked )
						if ( IsFriendlyBlocked( UpInt - i, SideInt + i ) )
							rightdown_blocked = true;
						else if ( !addedtarget_rightdown )
						{
							if ( !IsFriendlyBlocked( UpInt - i, SideInt + i, true ) )
								addedtarget_rightdown = true;

							moves.Add( ConcatInt( UpInt - i, SideInt + i ) );
						}
				}
			}
			else if ( PieceType == 5 ) // Queen
			{
				bool leftup_blocked = false;
				bool righttup_blocked = false;
				bool leftdown_blocked = false;
				bool rightdown_blocked = false;

				bool addedtarget_leftup = false;
				bool addedtarget_rightup = false;
				bool addedtarget_leftdown = false;
				bool addedtarget_rightdown = false;

				for ( int i = 1; i < 8; i++ )
				{
					if ( InBounds( UpInt + i, SideInt - i ) && !leftup_blocked )
						if ( IsFriendlyBlocked( UpInt + i, SideInt - i ) )
							leftup_blocked = true;
						else if ( !addedtarget_leftup )
						{
							if ( !IsFriendlyBlocked( UpInt + i, SideInt - i, true ) )
								addedtarget_leftup = true;

							moves.Add( ConcatInt( UpInt + i, SideInt - i ) );
						}


					if ( InBounds( UpInt + i, SideInt + i ) && !righttup_blocked )
						if ( IsFriendlyBlocked( UpInt + i, SideInt + i ) )
							righttup_blocked = true;
						else if ( !addedtarget_rightup )
						{
							if ( !IsFriendlyBlocked( UpInt + i, SideInt + i, true ) )
								addedtarget_rightup = true;

							moves.Add( ConcatInt( UpInt + i, SideInt + i ) );
						}

					if ( InBounds( UpInt - i, SideInt - i ) && !leftdown_blocked )
						if ( IsFriendlyBlocked( UpInt - i, SideInt - i ) )
							leftdown_blocked = true;
						else if ( !addedtarget_leftdown )
						{
							if ( !IsFriendlyBlocked( UpInt - i, SideInt - i, true ) )
								addedtarget_leftdown = true;

							moves.Add( ConcatInt( UpInt - i, SideInt - i ) );
						}

					if ( InBounds( UpInt - i, SideInt + i ) && !rightdown_blocked )
						if ( IsFriendlyBlocked( UpInt - i, SideInt + i ) )
							rightdown_blocked = true;
						else if ( !addedtarget_rightdown )
						{
							if ( !IsFriendlyBlocked( UpInt - i, SideInt + i, true ) )
								addedtarget_rightdown = true;

							moves.Add( ConcatInt( UpInt - i, SideInt + i ) );
						}
				}


				bool upblocked = false;
				bool downblocked = false;
				bool leftblocked = false;
				bool rightblocked = false;

				bool addedTargetup = false;
				bool addedTargetdown = false;
				bool addedTargetleft = false;
				bool addedTargetright = false;

				for ( int i = 1; i < 8; i++ )
				{
					var nextUp = UpInt + i;
					var nextDown = UpInt - i;
					var nextRight = SideInt + i;
					var nextLeft = SideInt - i;

					if ( InBounds( nextUp, SideInt ) && !upblocked )
						if ( IsFriendlyBlocked( nextUp, SideInt ) )
							upblocked = true;
						else if ( !addedTargetup )
						{
							if ( !IsFriendlyBlocked( nextUp, SideInt, true ) )
								addedTargetup = true;

							moves.Add( ConcatInt( nextUp, SideInt ) );
						}

					if ( InBounds( nextDown, SideInt ) && !downblocked )
						if ( IsFriendlyBlocked( nextDown, SideInt ) )
							downblocked = true;
						else if ( !addedTargetdown )
						{
							if ( !IsFriendlyBlocked( nextDown, SideInt, true ) )
								addedTargetdown = true;

							moves.Add( ConcatInt( nextDown, SideInt ) );
						}

					if ( InBounds( UpInt, nextLeft ) && !leftblocked )
						if ( IsFriendlyBlocked( UpInt, nextLeft ) )
							leftblocked = true;
						else if ( !addedTargetleft )
						{
							if ( !IsFriendlyBlocked( UpInt, nextLeft, true ) )
								addedTargetleft = true;

							moves.Add( ConcatInt( UpInt, nextLeft ) );
						}

					if ( InBounds( UpInt, nextRight ) && !rightblocked )
						if ( IsFriendlyBlocked( UpInt, nextRight ) )
							rightblocked = true;
						else if ( !addedTargetright )
						{
							if ( !IsFriendlyBlocked( UpInt, nextRight, true ) )
								addedTargetright = true;

							moves.Add( ConcatInt( UpInt, nextRight ) );
						}
				}
			}
			else if ( PieceType == 6 )
			{
				int[] wanted_moves = { ConcatInt( UpInt + 1, SideInt ), ConcatInt( UpInt + 1, SideInt + 1 ), ConcatInt( UpInt, SideInt + 1 ), ConcatInt( UpInt - 1, SideInt + 1 ), ConcatInt( UpInt - 1, SideInt ), ConcatInt( UpInt - 1, SideInt - 1 ), ConcatInt( UpInt, SideInt - 1 ), ConcatInt( UpInt + 1, SideInt - 1 ) };

				foreach ( var wanted in wanted_moves )
				{
					string num_str = wanted.ToString();

					if ( num_str.Length < 2 )
						continue;

					int up = (int)(num_str[0]) - 48;
					int side = (int)(num_str[1]) - 48;

					if ( InBounds( up, side ) && !IsFriendlyBlocked( up, side ) )
						moves.Add( wanted );
				}
			}

			foreach ( var result in moves )
			{
				string num_str = result.ToString();
				int up = (int)(num_str[0]) - 48;
				int side = (int)(num_str[1]) - 48;

				if ( up > 8 || up < 1 || side > 8 || side < 1 )
					continue;

				if ( IsClient && mark )
				{
					ChessGame.Current.SetMarkedCell( up, side, true, ChessGame.Current.GetOccupant(up, side).IsValid() );
				}
			}

			return moves;
		}

		public void SetType( int type )
		{
			SetModel( pieceTypes[type - 1] );

			PieceType = type;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}
	}
}
