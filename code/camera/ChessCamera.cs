namespace Chess
{
	using Sandbox;
	using System;

	public class ChessCamera : Camera
	{
		public int CameraMode { get; set; } = 0;
		public ChessCamera()
		{

		}

		Vector3[] positions = new Vector3[3] { new Vector3( -800f, 0f, 1900f ) , new Vector3( -1000f, 0f, 2000f ), new Vector3( -10f, 0f, 2300f ) };

		public override void Update()
		{
			FieldOfView = 70;

			var pos = positions[CameraMode];

			ChessPlayer pawn = Local.Pawn as ChessPlayer;
			bool isWhite = pawn.IsValid() && pawn.Team == 1;

			if ( isWhite )
			{
				pos.x = -pos.x;
				pos.y = -pos.y;
			}

			Position = pos;

			var targetDelta = (new Vector3( 0f, 0f, 1100f ) - Position);
			var targetDirection = targetDelta.Normal;

			Rotation = Rotation.From( new Angles(
			((float)Math.Asin( targetDirection.z )).RadianToDegree() * -1.0f,
			((float)Math.Atan2( targetDirection.y, targetDirection.x )).RadianToDegree(),
			0.0f ) );

			Viewer = null;
		}
	}
}
