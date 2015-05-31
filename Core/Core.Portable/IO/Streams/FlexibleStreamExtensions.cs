using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Core.Common;
using Core.IO.Terminal;

namespace Core.IO.Streams
{
	public static class FlexibleStreamExtensions
	{
		public static IFlexibleOutputStream ToFlexiblePipeTarget (StreamWriter streamWriter)
		{
			return new FlexibleStreamWriter (streamWriter);
		}

		public static async Task WriteAsync (this IFlexibleOutputStream flex, params object[] values)
		{
			foreach (object value in values) {
				//Log.Debug ("FlexibleStream.WriteAsync: ", value);
				await flex.WriteAsync (value.ToString ()).ConfigureAwait (false);
			}
		}

		public static async Task WriteLineAsync (this IFlexibleOutputStream flex, params object[] values)
		{
			foreach (object value in values) {
				//Log.Debug ("FlexibleStream.WriteAsync: ", value);
				await flex.WriteAsync (value.ToString ()).ConfigureAwait (false);
			}
			await flex.WriteAsync (System.Environment.NewLine);
		}

		public static async Task Eat (this IFlexibleOutputStream flex, StreamReader reader)
		{
			string line;
			while ((line = await reader.ReadLineAsync ()) != null) {

				Log.Debug ("received: ", line);
				await flex.WriteLineAsync (line);
			}
		}

		public static ReadLineHandler ToReadLineCallback (this IFlexibleOutputStream flex)
		{
			return async (line) => {
				if (line.SpecialCommand == SpecialCommands.CloseStream) {
					Log.Debug ("try close!");
					await flex.TryClose ();
				} else {
					Log.Debug ("eat: ", line);
					await flex.WriteLineAsync (line.Line).ConfigureAwait (false);
					Log.Debug ("eaten: ", line);
				}
			};
		}

		/*public static async Task Eat (, IReadLine readLine, CancellationToken cancelToken)
		{
			readLine.CancelToken = cancelToken;
			while (readLine.IsOpen && !cancelToken.IsCancellationRequested) {
				
				if (await readLine.TryReadLineAsync ().ConfigureAwait (false)) {
				}
			}

			readLine.CancelToken = CancellationToken.None;
		}*/


		public static TextWriter ToTextWriter (this IFlexibleOutputStream flex)
		{
			return new InternalTextWriter (flex);
		}

		private class InternalTextWriter : TextWriter
		{
			IFlexibleOutputStream flexi;

			public InternalTextWriter (IFlexibleOutputStream flexi)
			{
				this.flexi = flexi;
			}

			public override void Write (char value)
			{
				//Log.Debug ("InternalTextWriter.Write: ", value);
				flexi.WriteAsync (value + "").Wait ();
			}

			public override void Write (string value)
			{
				//Log.Debug ("InternalTextWriter.Write: ", value);
				flexi.WriteAsync (value).Wait ();
			}

			public override System.Text.Encoding Encoding {
				get { return System.Text.Encoding.UTF8; }
			}
		}
	}
}
