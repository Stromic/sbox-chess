namespace Chess
{
	using Sandbox;
	using Sandbox.UI;
	using Sandbox.UI.Construct;
	using System.Collections.Generic;

	public class Notifications : Panel
	{
		public Notifications()
		{
			StyleSheet.Load( "/ui/Notifications.scss" );
		}

		[Event( "ChessNotify" )]
		public void AddNotification(string msg)
		{
			var notif = new Notification();
			notif.SetText(msg);
			notif.Parent = this;
		}

		public override void Tick()
		{
			base.Tick();
		}
	}

	public class Notification : Label
	{
		private float created = Time.Now;
		private bool removing;
		
		private async void Remove()
		{
			removing = true;

			AddClass("removing");

			await Task.Delay(1000);

			Delete();
		}

		public override void Tick()
		{
			if (created + 1.5 < Time.Now && !removing )
			{
				Remove();
			}

			base.Tick();
		}
	}
}
