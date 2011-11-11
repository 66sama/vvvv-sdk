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
using SlimDX;

//the vvvv node namespace
namespace VVVV.Nodes
{
	
	//class definition
	public class SkeletonInputMorph: IPlugin, IDisposable
    {	          	
    	#region field declaration
    	
    	//the host (mandatory)
    	private IPluginHost FHost; 
    	// Track whether Dispose has been called.
   		private bool FDisposed = false;

    	private IValueConfig FPoseCount;
    	private IValueIn FInput;
    	private List<INodeIn> poseNodes;
    	
    	private INodeOut FPoseOutput;
    	
    	private IJoint outputJoint;
    	private Skeleton outputSkeleton;
    	
    	private List<IJoint> poses;
    	private double input;
		double poseCount = 2;
		private int passthrough = -1; // if an input pose is just passed through, its index is saved here, -1 if there's no passthrough
		
    	
    	#endregion field declaration
       
    	#region constructor/destructor
    	
        public SkeletonInputMorph()
        {
			//the nodes constructor
			//nothing to declare for this node
			outputSkeleton = new Skeleton();
			
			poses = new List<IJoint>();
			poseNodes = new List<INodeIn>();
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
        ~SkeletonInputMorph()
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
					FPluginInfo.Name = "InputMorph";
					//the nodes category: try to use an existing one
					FPluginInfo.Category = "Skeleton";
					//the nodes version: optional. leave blank if not
					//needed to distinguish two nodes of the same name and category
					FPluginInfo.Version = "";
					
					//the nodes author: your sign
					FPluginInfo.Author = "Matthias Zauner";
					//describe the nodes function
					FPluginInfo.Help = "InputMorph";
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
	    	
	    	FHost.CreateValueConfig("Number of Input Poses", 1, null, TSliceMode.Single, TPinVisibility.OnlyInspector, out FPoseCount);
	    	FPoseCount.SetSubType(2, 10, 1, 2, false, false, true);
	    	
	    	FHost.CreateValueInput("Input", 1, null, TSliceMode.Single, TPinVisibility.True, out FInput);
		    FInput.SetSubType(0.0, 1.0, 0.01, 0.0, false, false, false);
	    	
	    	INodeIn currPoseNode;
	    	for (int i=0; i<2; i++)
    		{
    			FHost.CreateNodeInput("Pose "+(i+1), TSliceMode.Single, TPinVisibility.True, out currPoseNode);
		    	currPoseNode.SetSubType(guids, "Skeleton");
		    	poseNodes.Add(currPoseNode);
		    	poses.Add(null);
    		}
	    	
	    	// create outputs
	    	
	    	FHost.CreateNodeOutput("Mixed Pose", TSliceMode.Single, TPinVisibility.True, out FPoseOutput);
	    	FPoseOutput.SetSubType(guids, "Skeleton");
	    	
	    	
        }

        #endregion pin creation
        
        #region mainloop
        
        public void Configurate(IPluginConfig Input)
        {
        	//nothing to configure in this plugin
        	//only used in conjunction with inputs of type cmpdConfigurate
        	
        	if (Input.Name == "Number of Input Poses")
        	{
        		System.Guid[] guids = new System.Guid[1];
	    		guids[0] = new Guid("AB312E34-8025-40F2-8241-1958793F3D39");
	    		
        		IValueConfig valueInput = (IValueConfig)Input;
        		valueInput.GetValue(0, out poseCount);
        		
        		FInput.SetSubType(0, poseCount-1, 0.01, 0.0, false, false, false);
        		
        		int oldPoseCount = poseNodes.Count;
        		for (int i=oldPoseCount-1; i>=(int)poseCount; i--)
        		{
        			FHost.DeletePin(poseNodes[i]);
        		}
        		for (int i=oldPoseCount-1; i>=(int)poseCount; i--)
        		{
        			poseNodes.RemoveAt(i);
        			poses.RemoveAt(i);
        		}
        		
        		INodeIn currPoseNode;
        		for (int i=oldPoseCount; i<(int)poseCount; i++)
        		{
        			FHost.CreateNodeInput("Pose "+(i+1), TSliceMode.Single, TPinVisibility.True, out currPoseNode);
			    	currPoseNode.SetSubType(guids, "Skeleton");
			    	poseNodes.Add(currPoseNode);
			    	poses.Add(null);
        		}
		    	
        	}
        }
        
        //here we go, thats the method called by vvvv each frame
        //all data handling should be in here
        public void Evaluate(int SpreadMax)
        {     	
        	//if any of the inputs has changed
        	//recompute the outputs
        	bool recalculate = false;
        	
        	if (FInput.PinIsChanged)
        	{
        		FInput.GetValue(0, out input);
        		recalculate = true;
        	}
        	
            INodeIOBase currInterface;
            for (int i=0; i<poseNodes.Count; i++)
            {
            	if (poseNodes[i].PinIsChanged)
            	{
            		if (poseNodes[i].IsConnected)
            		{
	            		poseNodes[i].GetUpstreamInterface(out currInterface);
	            		IJoint currPose = ((Skeleton)currInterface).Root;
	            		poses[i] = currPose;
            		}
            		else
            		{
            			poses[i] = null;
            			if (i==0)
            				outputJoint = null;
            		}
            		recalculate = true;
            	}
            	
            }
        	
        	if (recalculate)
        	{
        		int index1 = (int)Math.Floor(input);
        		int index2 = (int)Math.Min(Math.Ceiling(input), poseCount-1);
        		double amount2 = input - index1;
        		double amount1 = (input - index2)*-1;
				
        		if (amount2==0)
        		{
        			outputJoint = poses[index1];
        			if (passthrough!=index1)
        			{
        				outputSkeleton.Root = outputJoint;
        				outputSkeleton.BuildJointTable();
        			}
        			passthrough = index1;
        		}
        		else if (amount1==0)
        		{
        			outputJoint = poses[index2];
        			if (passthrough!=-1)
        			{
        				outputSkeleton.Root = outputJoint;
        				outputSkeleton.BuildJointTable();
        			}
        			passthrough = index2;
        		}
        		else
        		{
        			if (outputJoint==null || passthrough>=0)
        			{
	        			outputJoint = poses[index1].DeepCopy();
	        			outputSkeleton.Root = outputJoint;
						outputSkeleton.BuildJointTable();
        			}
        			else
        				copyAttributes(poses[index1], outputJoint);
	        		mixJoints(outputJoint, poses[index1], amount1, poses[index2], amount2);
	        		passthrough = -1;
        		}
   
				outputSkeleton.Root = outputJoint;
        		FPoseOutput.MarkPinAsChanged();
        	}
        
			
        	FPoseOutput.SetInterface(outputSkeleton);
        	
        }
             
        #endregion mainloop  
        
        #region helper
        
        private void mixJoints(IJoint result, IJoint joint1, double amount1, IJoint joint2, double amount2)
        {       	
        	double enableJoint1 = 1.0;
        	double enableJoint2 = 1.0;
        	Vector3D p;
        	p = joint1.AnimationTransform * new Vector3D(1, 1, 1); // check, if AnimationTransform is Identity Matrix... for the lazy ones...
        	if (p.x == 1 && p.y==1 && p.z==1)
        		enableJoint1 = 0;
        	p = joint2.AnimationTransform * new Vector3D(1, 1, 1);
        	if (p.x == 1 && p.y==1 && p.z==1)
        		enableJoint2 = 0;

        	Matrix4x4 resultAnimationT = result.AnimationTransform;
        	Matrix4x4Utils.Blend(joint1.AnimationTransform, joint2.AnimationTransform, amount1 * enableJoint1, amount2 * enableJoint2, out resultAnimationT);
   
        	result.AnimationTransform = resultAnimationT;
        	
        	for (int i=0; i<result.Children.Count; i++)
        	{
        		mixJoints(result.Children[i], joint1.Children[i], amount1, joint2.Children[i], amount2);
        	}
        }
        
        private void copyAttributes(IJoint fromJoint, IJoint toJoint)
        {
        	toJoint.AnimationTransform = fromJoint.AnimationTransform;
        	toJoint.BaseTransform = fromJoint.BaseTransform;
        	toJoint.Constraints = fromJoint.Constraints;
        	
        	for (int i=0; i<fromJoint.Children.Count; i++)
        	{
        		copyAttributes(fromJoint.Children[i], toJoint.Children[i]);
        	}
        }
        
        #endregion helper
        
        
        
	}
}
