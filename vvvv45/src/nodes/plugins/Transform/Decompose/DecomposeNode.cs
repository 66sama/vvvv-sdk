#region usings
using System;
using System.ComponentModel.Composition;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using VVVV.Utils.SlimDX;
using VVVV.Core.Logging;
using SlimDX;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Decompose", 
        Category = "Transform", 
        Version = "Vector", 
        Help = "Decomposes a matrix into translation, scaling and euler angles",
        Tags = "extract, retrieve, translation, scaling, rotation",
        Author = "vvvv group")]
	#endregion PluginInfo
	public class DecomposeNode : IPluginEvaluate
	{
		#region fields & pins
		[Input("Transform")]
		IDiffSpread<Matrix> FInput;

		[Output("Translate ")]
		ISpread<Vector3> FTransOut;
		
		[Output("Scale ")]
		ISpread<Vector3> FScaleOut;
		
		[Output("Rotate ")]
		ISpread<Vector3D> FRotOut;

        [Output("OK")]
        ISpread<bool> FOKOut;

		[Import()]
		ILogger FLogger;
		#endregion fields & pins

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			FTransOut.SliceCount = SpreadMax;
			FScaleOut.SliceCount = SpreadMax;
			FRotOut.SliceCount = SpreadMax;

			if(FInput.IsChanged)
			{
				for (int i = 0; i < SpreadMax; i++)
				{
					Quaternion q;
					Vector3 scale;
					Vector3 trans;
					FOKOut[i] = FInput[i].Decompose(out scale, out q, out trans);
					
					FTransOut[i] = trans;
					FScaleOut[i] = scale;
					FRotOut[i] = VMath.QuaternionToEulerYawPitchRoll(q.ToVector4D()) * VMath.RadToCyc;
				}	
			}

			//FLogger.Log(LogType.Debug, "Logging to Renderer (TTY)");
		}
	}
}
