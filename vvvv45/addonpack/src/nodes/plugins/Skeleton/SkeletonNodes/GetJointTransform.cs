#region licence/info

//////project name
//vvvv plugin template

//////description
//basic vvvv node plugin template.
//Copy this an rename it, to write your own plugin node.

//////licence
//GNU Lesser General Public License (LGPL)
//english: http://www.gnu.org/licenses/lgpl.html
//german: http://www.gnu.de/lgpl-ger.html

//////language/ide
//C# sharpdevelop 

//////dependencies
//VVVV.PluginInterfaces.V1;
//VVVV.Utils.VColor;
//VVVV.Utils.VMath;

//////initial author
//vvvv group

#endregion licence/info

//use what you need
using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using VVVV.PluginInterfaces.V1;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;
using System.Text.RegularExpressions;
using System.Globalization;
using VVVV.Utils.SharedMemory;
using VVVV.SkeletonInterfaces;

//the vvvv node namespace
namespace VVVV.Nodes
{
	
	//class definition
	public class GetJointTransform: IPlugin, IDisposable
    {	          	
    	#region field declaration
    	
    	//the host (mandatory)
    	private IPluginHost FHost; 
    	// Track whether Dispose has been called.
   		private bool FDisposed = false;

    	private INodeIn FSkeletonInput;
    	private ITransformIn FInverseBindPoseInput;
    	private IStringIn FJointNameInput;
    	private IEnumIn FOutputModeInput;
    	
    	public static int OUTPUT_MODE_DYNAMIC = 0;
    	public static int OUTPUT_MODE_STATIC = 1;
    	
    	private ITransformOut FTransformOutput;
    	private Skeleton inputSkeleton;
    	private List<string> jointNames;
    	private int outputMode;
    	private bool jointsSelected = false;
    	
    	
    	#endregion field declaration
       
    	#region constructor/destructor
    	
        public GetJointTransform()
        {
			//the nodes constructor
			//nothing to declare for this node
			
		}
        
        // Implementing IDisposable's Dispose method.
        // Do not make this method virtual.
        // A derived class should not be able to override this method.
        public void Dispose()
        {
        	Dispose(true);
        	// Take yourself off the Finalization queue
        	// to prevent finalization code for this object
        	// from executing a second time.
        	GC.SuppressFinalize(this);
        }
        
        // Dispose(bool disposing) executes in two distinct scenarios.
        // If disposing equals true, the method has been called directly
        // or indirectly by a user's code. Managed and unmanaged resources
        // can be disposed.
        // If disposing equals false, the method has been called by the
        // runtime from inside the finalizer and you should not reference
        // other objects. Only unmanaged resources can be disposed.
        protected virtual void Dispose(bool disposing)
        {
        	// Check to see if Dispose has already been called.
        	if(!FDisposed)
        	{
        		if(disposing)
        		{
        			// Dispose managed resources.
        		}
        		// Release unmanaged resources. If disposing is false,
        		// only the following code is executed.
	        	
        		FHost.Log(TLogType.Debug, "PluginTemplate is being deleted");
        		
        		// Note that this is not thread safe.
        		// Another thread could start disposing the object
        		// after the managed resources are disposed,
        		// but before the disposed flag is set to true.
        		// If thread safety is necessary, it must be
        		// implemented by the client.
        	}
        	FDisposed = true;
        }

        // Use C# destructor syntax for finalization code.
        // This destructor will run only if the Dispose method
        // does not get called.
        // It gives your base class the opportunity to finalize.
        // Do not provide destructors in types derived from this class.
        ~GetJointTransform()
        {
        	// Do not re-create Dispose clean-up code here.
        	// Calling Dispose(false) is optimal in terms of
        	// readability and maintainability.
        	Dispose(false);
        }
        #endregion constructor/destructor
        
        #region node name and infos
       
        //provide node infos 
        private static IPluginInfo FPluginInfo;
        public static IPluginInfo PluginInfo
	    {
	        get 
	        {
	        	if (FPluginInfo == null)
				{
					//fill out nodes info
					//see: http://www.vvvv.org/tiki-index.php?page=vvvv+naming+conventions
					FPluginInfo = new PluginInfo();
					
					//the nodes main name: use CamelCaps and no spaces
					FPluginInfo.Name = "GetJointTransform";
					//the nodes category: try to use an existing one
					FPluginInfo.Category = "Skeleton";
					//the nodes version: optional. leave blank if not
					//needed to distinguish two nodes of the same name and category
					FPluginInfo.Version = "";
					
					//the nodes author: your sign
					FPluginInfo.Author = "Matthias Zauner";
					//describe the nodes function
					FPluginInfo.Help = "GetJointTransform";
					//specify a comma separated list of tags that describe the node
					FPluginInfo.Tags = "";
					
					//give credits to thirdparty code used
					FPluginInfo.Credits = "";
					//any known problems?
					FPluginInfo.Bugs = "certainly";
					//any known usage of the node that may cause troubles?
					FPluginInfo.Warnings = "";
					
					//leave below as is
					System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
					System.Diagnostics.StackFrame sf = st.GetFrame(0);
					System.Reflection.MethodBase method = sf.GetMethod();
					FPluginInfo.Namespace = method.DeclaringType.Namespace;
					FPluginInfo.Class = method.DeclaringType.Name;
					//leave above as is
				}
				return FPluginInfo;
	        }
		}

        public bool AutoEvaluate
        {
        	//return true if this node needs to calculate every frame even if nobody asks for its output
        	get {return false;}
        }
        
        #endregion node name and infos
        
      	#region pin creation
        
        //this method is called by vvvv when the node is created
        public void SetPluginHost(IPluginHost Host)
	    {
        	//assign host
	    	FHost = Host;
	    	
	    	System.Guid[] guids = new System.Guid[1];
	    	guids[0] = new Guid("AB312E34-8025-40F2-8241-1958793F3D39");

	    	//create inputs
	    	FHost.CreateNodeInput("Skeleton", TSliceMode.Single, TPinVisibility.True, out FSkeletonInput);
	    	FSkeletonInput.SetSubType(guids, "Skeleton");
	    	
	    	FHost.CreateTransformInput("Inverse Bind Pose", TSliceMode.Dynamic, TPinVisibility.True, out FInverseBindPoseInput);
	    	
	    	FHost.UpdateEnum("SkinningMatricesOutputMode", "Dynamic", new string[]{"Dynamic", "Fixed to 60"});
	    	FHost.CreateEnumInput("Output Transform Count", TSliceMode.Single, TPinVisibility.True, out FOutputModeInput);
	    	FOutputModeInput.SetSubType("SkinningMatricesOutputMode");
	    	
	    	FHost.CreateStringInput("Joint Name", TSliceMode.Dynamic, TPinVisibility.True, out FJointNameInput);
	    	
	    	// create outputs
	    	
	    	FHost.CreateTransformOutput("Transform", TSliceMode.Dynamic, TPinVisibility.True, out FTransformOutput);
	    	
	    	
	    	
        }

        #endregion pin creation
        
        #region mainloop
        
        public void Configurate(IPluginConfig Input)
        {
        	//nothing to configure in this plugin
        	//only used in conjunction with inputs of type cmpdConfigurate
        }
        
        //here we go, thats the method called by vvvv each frame
        //all data handling should be in here
        public void Evaluate(int SpreadMax)
        {     	
        	//if any of the inputs has changed
        	//recompute the outputs
        	
        	bool recalculate = false;
        	
        	if (FJointNameInput.PinIsChanged)
        	{
        		jointNames = new List<string>();
        		string jointName;
        		for (int i=0; i<FJointNameInput.SliceCount; i++)
        		{
        			FJointNameInput.GetString(i, out jointName);
        			jointNames.Add(jointName);
        		}
        		recalculate = true;
        		if (jointNames.Count==1 && string.IsNullOrEmpty(jointNames[0]))
        			jointsSelected = false;
        		else
        			jointsSelected = true;
        	}
        	
        	if (FSkeletonInput.PinIsChanged)
        	{
        		if (FSkeletonInput.IsConnected)
        		{
	        		INodeIOBase currInterface;
	        		FSkeletonInput.GetUpstreamInterface(out currInterface);
	        		inputSkeleton = (Skeleton)currInterface;
	        		
	        		// if there are no specific joints selected via input pin, collect them all
	        		if (jointNames==null || !jointsSelected)
	        		{
	        			jointNames = new List<string>();
	        			foreach (KeyValuePair<string, IJoint> pair in inputSkeleton.JointTable)
	        			{
	        				// Only add those with a valid array index.
	    					// It's not a must that all bones are used as skinning matrices.
	        				if (pair.Value.Id >= 0)
	        					jointNames.Add(pair.Key);
	        			}
	        		}
        		}
        		else
        			inputSkeleton = null;
        		recalculate = true;
        	}
        	
        	if (FInverseBindPoseInput.PinIsChanged)
        	{
        		recalculate = true;
        	}
        	
        	if (FOutputModeInput.PinIsChanged)
        	{
        		FOutputModeInput.GetOrd(0, out outputMode);
        		recalculate = true;
        	}
        	
        	if (recalculate && inputSkeleton!=null)
        	{
        		inputSkeleton.CalculateCombinedTransforms();
        		int jointCount;
        		if (outputMode == OUTPUT_MODE_DYNAMIC)
        			jointCount = jointNames.Count;
        		else
        			jointCount = 60;
        		FTransformOutput.SliceCount = jointCount;
        		IJoint currJoint;
        		Matrix4x4 currIBPMatrix;
        		int i=0;
        		for (i=0; i<jointNames.Count; i++)
        		{
        			currJoint = inputSkeleton.JointTable[jointNames[i]];
        			if (currJoint!=null)
        			{
        				int sliceIndex;
        				if (outputMode == OUTPUT_MODE_STATIC)
        					sliceIndex = currJoint.Id;
        				else
        					sliceIndex = i;
        				FInverseBindPoseInput.GetMatrix(sliceIndex, out currIBPMatrix);
        				FTransformOutput.SetMatrix(sliceIndex, currIBPMatrix * currJoint.CombinedTransform);
        			}
        		}
        		
        		// Pad remaining slices with Identity Matrices
        		for (int j=i; j<jointCount; j++)
        		{
        			FTransformOutput.SetMatrix(j, VMath.IdentityMatrix);
        		}
        	}
        
        }
             
        #endregion mainloop  
        
        #region helper
        

        
        #endregion helper
        
        
        
	}
}
