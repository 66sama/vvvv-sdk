// PluginTemplateCpp.h

#pragma once

using namespace System;
using namespace VVVV::PluginInterfaces::V1;

namespace MyNodes {

	public ref class StructureSynthNode :IPlugin
	{
		private:
			IPluginHost^ FHost;

			IStringIn^ vInFormula;
			IValueIn^ vInSeed;


			ITransformOut^ vOutSphere;
			IColorOut^ vOutSphereColor;

			ITransformOut^ vOutBoxes;
			IColorOut^ vOutBoxesColor;

			ITransformOut^ vOutGrid;
			IColorOut^ vOutGridColor;

			IValueOut^ vOutLines;
			IColorOut^ vOutLinesColor;

			IStringOut^ vOutMessage;

		public:
			StructureSynthNode() { }
			~StructureSynthNode() { }

			static property IPluginInfo^ PluginInfo 
			{
				IPluginInfo^ get() 
				{
					//IPluginInfo^ Info;
					IPluginInfo^ Info = gcnew VVVV::PluginInterfaces::V1::PluginInfo();
					Info->Name = "StructureSynth";
					Info->Category = "Spreads";
					Info->Version = "";
					Info->Help = "Structure Synth renderer";
					Info->Bugs = "";
					Info->Credits = "";
					Info->Warnings = "";

					//leave below as is
					System::Diagnostics::StackTrace^ st = gcnew System::Diagnostics::StackTrace(true);
					System::Diagnostics::StackFrame^ sf = st->GetFrame(0);
					System::Reflection::MethodBase^ method = sf->GetMethod();
					Info->Namespace = method->DeclaringType->Namespace;
					Info->Class = method->DeclaringType->Name;
					return Info;
				}
			}
			virtual void SetPluginHost(IPluginHost^ Host);
			virtual void Configurate(IPluginConfig^ Input);
			virtual void Evaluate(int SpreadMax);
			
			virtual property bool AutoEvaluate {
				bool get() { return false; }
			}


	};
}
