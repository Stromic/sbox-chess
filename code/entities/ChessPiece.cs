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

		[Net]
		public int KingChecked { get; set; } = 0;

		[Net]
		public bool IsDangered { get; set; }
		[Net]
		public bool Killed { get; set; }

		public int virtualized_up { get; set; }
		public int virtualized_side { get; set; }

		private int ConcatInt(int a, int b ) // Yeah very nice
		{
			if (a < 0 || b < 0 )
			{
				return 0;
			}

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
				if ( piece.Team == Team || piece.Killed )
					continue;

				var moves = piece.GetMoves(false, false, null, null, true);

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

			return PieceType == 6 ? false : result;
		}

		public Dictionary<int, bool> CanShieldKingMoves()
		{
			var game = ChessGame.Current;
			var king = (Team == 1 ? game.white_player : game.black_player)?.King;
			var danger = king?.InDanger();

			var blockers = new Dictionary<int, bool>(); ;

			if ( danger.IsValid() && PieceType != 6 )
			{
				danger.GetMoves( false, false, GetMoves(), blockers );
			}

			return blockers;
		}

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

			if ( !this.IsValid() )
				return;

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
					var game = ChessGame.Current;
					var ply = IsBlack ? game.black_player : game.white_player;
					ply.PromotingPiece = this;

					ply.SetPromotionScreen( To.Single( ply ), true );

					return false;
				}

				return true;
			}

			return true;
		}

		private void GetGridsInDir( List<int> grids, int type, int iterations = 8, bool ignore_occupied = false, List<int> blockers = null, Dictionary<int, bool> blockers_fill = null, bool ignore_putdanger = false )
		{
			bool IsBlack = Team == 2;
			bool KingOccupied = false;

			for ( int i = 1; i < iterations; i++ )
			{
				int up = UpInt;
				int side = SideInt;

				if (type == 1 ) // Rook Logics
				{
					up = UpInt + (IsBlack ? -i : i); // Forward
				} else if (type == 2 )
				{
					up = UpInt + (IsBlack ? i : -i); // Backward
				} else if (type == 3 )
				{
					side = SideInt + (IsBlack ? i : -i); // Left
				} else if (type == 4)
				{
					side = SideInt + (IsBlack ? -i : i); // Right
				}
				else if ( type == 5 ) // Bishop Logics
				{
					up = UpInt + (IsBlack ? -i : i);  // Left Top
					side = SideInt + (IsBlack ? i : -i);
				}
				else if ( type == 6 )
				{
					up = UpInt + (IsBlack ? -i : i);  // Right Top
					side = SideInt + (IsBlack ? -i : i);
				}
				else if ( type == 7 )
				{
					up = UpInt + (IsBlack ? i : -i);  // Left Bottom
					side = SideInt + (IsBlack ? i : -i);
				}
				else if ( type == 8 )
				{
					up = UpInt + (IsBlack ? i : -i);  // Right Bottom
					side = SideInt + (IsBlack ? -i : i);
				}

				var checkBlockers = blockers != null && blockers_fill != null;

				if ( checkBlockers && KingChecked == type )
				{
					bool blocked = false;

					foreach ( var move in blockers )
					{
						string num_str = move.ToString();
						int mv_up = (int)(num_str[0]) - 48;
						int mv_side = (int)(num_str[1]) - 48;

						if (mv_up == up && mv_side == side && !KingOccupied )
						{
							blockers_fill[move] = true;
							blocked = true;
							break;
						}
					}

					if (blocked)
						break;
				}

				if ( !ignore_putdanger && WillMovePutKingInDanger( up, side ) )
					break;

				if (!InBounds( up, side ) || IsFriendlyBlocked( up, side ) && !ignore_occupied)
					break;

				var occupy = ChessGame.Current.GetOccupant( up, side );

				grids.Add( ConcatInt( up, side ) );

				var isKing = (occupy.IsValid() && occupy.PieceType == 6);
				if ( isKing && occupy.PieceType == 6 )
					KingChecked = type;

				if ( isKing )
					KingOccupied = true;

				if ( ChessGame.Current.IsCellOccupied( up, side ) && !isKing )
					break;
			}
		}

		public List<int> GetSafeMoves( bool mark = false )
		{
			var moves = GetMoves();

			var badMoves = new Dictionary<int, bool>(); ;
			List<int> goodMoves = new List<int>();

			foreach ( var ent in Entity.All.OfType<ChessPiece>() )
			{
				if ( ent.Team == Team || ent.Killed )
					continue;

				var ent_moves = ent.GetMoves( false, true );

				foreach ( var move in ent_moves )
				{
					badMoves[move] = true;
				}
			}

			foreach ( var move in moves )
			{
				if ( !badMoves.ContainsKey( move ) )
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

		public void Kill()
		{
			Killed = true;

			int AmountLeft = 0;

			foreach ( ChessPiece ent in Entity.All.OfType<ChessPiece>() )
			{
				if ( ent.Team == Team && !ent.Killed)
					AmountLeft = AmountLeft + 1;
			}

			var CurrentDeath = 16 - AmountLeft;
			var Doubled = CurrentDeath > 8;
			var CurPos = Doubled ? CurrentDeath - 8 : CurrentDeath;

			CurPos = Team == 1 ? 8 - (CurPos - 1) : CurPos;

			Vector3 pos = ChessGame.Current.GetPiecePosition( CurPos, Team == 2 ? 1 : 8 );
			pos.y += (Team == 2 ? 150f : -150f) * (Doubled ? 1.6f : 1);
			pos.z = 1587f;

			Position = pos;
		}

		public bool WillMovePutKingInDanger(int up, int side) // NOTE: Maybe this can be optimized
		{
			bool result = false;
			var game = ChessGame.Current;

			var king = Team == 1 ? game.white_king : game.black_king;

			virtualized_up = up;
			virtualized_side = side;

			if ( king.InDanger().IsValid() ) 
				result = true;

			virtualized_up = 0;
			virtualized_side = 0;

			return result;
		}

		public List<int> GetMoves( bool mark = false, bool ignore_blocks = false, List<int> blockers = null, Dictionary<int, bool> blockers_fill = null, bool ignore_putdanger = false )
		{
			List<int> moves = new List<int>();

			bool IsBlack = Team == 2;
			if ( PieceType == 1 ) // Pawn
			{
				if ( !ChessGame.Current.IsCellOccupied( UpInt + (IsBlack ? 1 : -1), SideInt ) && !ignore_blocks )
				{
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt ) );

					if ( FirstMove && !ChessGame.Current.IsCellOccupied( UpInt + (IsBlack ? 2 : -2), SideInt ) )
					{
						moves.Add( ConcatInt( UpInt + (IsBlack ? 2 : -2), SideInt ) );
					}
				}

				if ( (!IsFriendlyBlocked( UpInt + (IsBlack ? 1 : -1), SideInt - 1, true ) || ignore_blocks) && (!ignore_putdanger && !WillMovePutKingInDanger( UpInt + (IsBlack ? 1 : -1), SideInt - 1 )) )
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt - 1 ) );

				if ( (!IsFriendlyBlocked( UpInt + (IsBlack ? 1 : -1), SideInt + 1, true ) || ignore_blocks) && (!ignore_putdanger && !WillMovePutKingInDanger( UpInt + (IsBlack ? 1 : -1), SideInt + 1 )) )
					moves.Add( ConcatInt( UpInt + (IsBlack ? 1 : -1), SideInt + 1 ) );
			}
			else if ( PieceType == 2 ) // Rook
			{
				GetGridsInDir( moves, 1, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 2, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 3, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 4, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
			}
			else if ( PieceType == 3 ) // Horse
			{
				int[] horse_moves = { ConcatInt( UpInt + 2, SideInt - 1 ), ConcatInt( UpInt + 2, SideInt + 1 ), ConcatInt( UpInt - 2, SideInt - 1 ), ConcatInt( UpInt - 2, SideInt + 1 ), ConcatInt( UpInt + 1, SideInt - 2 ), ConcatInt( UpInt - 1, SideInt - 2 ), ConcatInt( UpInt + 1, SideInt + 2 ), ConcatInt( UpInt - 1, SideInt + 2 ) };

				foreach(var move in horse_moves )
				{
					if ( move < 0 )
						continue;

					string num_str = move.ToString();
		
					if ( num_str.Length < 2 )
						continue;

					int up = (int)(num_str[0]) - 48;
					int side = (int)(num_str[1]) - 48;

					if ( InBounds( up, side ) && !IsFriendlyBlocked( up, side ) )
						moves.Add( ConcatInt( up, side ) );
				}
			}
			else if ( PieceType == 4 ) // Bishop
			{
				GetGridsInDir( moves, 5, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 6, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 7, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 8, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
			}
			else if ( PieceType == 5 ) // Queen
			{
				GetGridsInDir( moves, 1, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 2, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 3, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 4, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 5, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 6, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 7, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
				GetGridsInDir( moves, 8, 8, ignore_blocks, blockers, blockers_fill, ignore_putdanger );
			}
			else if ( PieceType == 6 ) // King
			{
				int[] king_moves = { ConcatInt( UpInt + 1, SideInt ), ConcatInt( UpInt + 1, SideInt + 1 ), ConcatInt( UpInt, SideInt + 1 ), ConcatInt( UpInt - 1, SideInt + 1 ), ConcatInt( UpInt - 1, SideInt ), ConcatInt( UpInt - 1, SideInt - 1 ), ConcatInt( UpInt, SideInt - 1 ), ConcatInt( UpInt + 1, SideInt - 1 ) };

				foreach ( var wanted in king_moves )
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

				if ( result < 0 || num_str.Length < 2 )
					continue;

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

			if ( type == 6 )
			{
				var game = ChessGame.Current;


				if ( Team == 1  )
					game.white_king = this;
				else
					game.black_king = this;
			} 
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
		}
	}
}
