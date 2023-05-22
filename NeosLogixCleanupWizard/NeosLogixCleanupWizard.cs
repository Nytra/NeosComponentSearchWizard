using System.Collections.Generic;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using CodeX;
using HarmonyLib;

namespace NeosLogixCleanupWizard {
	public class NeosLogixCleanupWizard : NeosMod {
		public override string Name => "Component Wizard";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/XDelta/NeosLogixCleanupWizard";

		public override void OnEngineInit() {
			/*Harmony harmony = new Harmony("tk.deltawolf.LogixCleanupWizard");
			harmony.PatchAll();*/
			Harmony harmony = new Harmony("owo.Nytra.ComponentWizard");
			Engine.Current.RunPostInit(AddMenuOption);
		} 
		void AddMenuOption() {
			DevCreateNewForm.AddAction("Editor", "Nytra Wizard", (x) => LogixCleanupWizard.GetOrCreateWizard(x));
		}

		class LogixCleanupWizard {
			public static LogixCleanupWizard GetOrCreateWizard(Slot x) {
				if (_Wizard != null) {
					WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
					return _Wizard;
				} else {
					return new LogixCleanupWizard(x);
				}
			}
			static LogixCleanupWizard _Wizard;
			static Slot WizardSlot;

			readonly ReferenceField<Slot> processingRoot;
			readonly ReferenceField<Component> componentField;

			readonly ValueField<bool> ignoreGenericTypes;
			readonly ValueField<bool> showDetails;
			readonly ValueField<bool> confirmDestroy;

			readonly Text statusText;
			void UpdateStatusText(string info) {
				statusText.Content.Value = info;
			}

			string GetSlotParentHierarchyString(Slot slot, bool reverse = true)
			{
				string str;
				List<Slot> parents = new List<Slot>();

				slot.ForeachParent((parent) =>
				{
					parents.Add(parent);
				});

				if (reverse)
				{
					str = "";
					parents.Reverse();
					bool first = true;
					foreach (Slot s in parents)
					{
						if (first)
						{
							str += s.Name;
							first = false;
						}
						else
						{
							str += "/" + s.Name;
						}
					}
					str += "/" + slot.Name;
				}
				else
				{
					str = slot.Name;
					foreach (Slot s in parents)
					{
						str += "/" + s.Name;
					}
				}

				return str;
			}

			bool ValidateWizard()
			{
				if (processingRoot.Reference.Target == null)
				{
					UpdateStatusText("No processing root provided!");
					return false;
				}

				if (componentField.Reference.Target == null)
				{
					UpdateStatusText("No component provided!");
					return false;
				}

				return true;
			}

			LogixCleanupWizard(Slot x) {
				_Wizard = this;

				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.OnPrepareDestroy += Slot_OnPrepareDestroy;
				WizardSlot.PersistentSelf = false;

				NeosCanvasPanel canvasPanel = WizardSlot.AttachComponent<NeosCanvasPanel>();
				canvasPanel.Panel.AddCloseButton();
				canvasPanel.Panel.AddParentButton();
				canvasPanel.Panel.Title = "Component Wizard";
				canvasPanel.Canvas.Size.Value = new float2(400f, 390f);

				Slot Data = WizardSlot.AddSlot("Data");
				this.processingRoot = Data.AddSlot("processingRoot").AttachComponent<ReferenceField<Slot>>();
				componentField = Data.AddSlot("componentField").AttachComponent<ReferenceField<Component>>();
				ignoreGenericTypes = Data.AddSlot("ignoreGenericTypes").AttachComponent<ValueField<bool>>();
				showDetails = Data.AddSlot("showDetails").AttachComponent<ValueField<bool>>();
				showDetails.Value.Value = true;
				confirmDestroy = Data.AddSlot("confirmDestroy").AttachComponent<ValueField<bool>>();

				UIBuilder UI = new UIBuilder(canvasPanel.Canvas);
				UI.Canvas.MarkDeveloper();
				UI.Canvas.AcceptPhysicalTouch.Value = false;
				VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;
				UI.Style.MinHeight = 24f;
				UI.Style.PreferredHeight = 24f;

				UI.Text("Processing Root:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.Next("Root");
				UI.Current.AttachComponent<RefEditor>().Setup(processingRoot.Reference);

				UI.Text("Component Field:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.Next("Component");
				UI.Current.AttachComponent<RefEditor>().Setup(componentField.Reference);

				UI.HorizontalElementWithLabel("Ignore Generic Type Arguments:", 0.942f, () => UI.BooleanMemberEditor(ignoreGenericTypes.Value));
				UI.HorizontalElementWithLabel("Show Details:", 0.942f, () => UI.BooleanMemberEditor(showDetails.Value));

				//UI.Text("String Field:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				//UI.TextField().Text.Content.OnValueChange += (field) => stringField.Value.Value = field.Value;

				//testButton = UI.Button("Make an explode owo");
				//testButton.LocalPressed += (IButton button, ButtonEventData data) => 
				//{
				//	Slot explodeSlot = WizardSlot.World.RootSlot.AddSlot("explode");
				//	explodeSlot.AttachComponent<ViolentAprilFoolsExplosion>();
				//	explodeSlot.Destroy();
				//};

				//UI.Text("----------");

				var searchButton = UI.Button("Search");
				searchButton.LocalPressed += (IButton button, ButtonEventData data) =>
				{

					if (!ValidateWizard()) return;

					int count = 0;
					string text = "";
					if (ignoreGenericTypes.Value.Value == true)
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType().Name == componentField.Reference.Target.GetType().Name))
						{
							count++;
							text += c.GetType().GetNiceName() + " - " + (c.Enabled ? "Enabled" : "Disabled") + " - " + GetSlotParentHierarchyString(c.Slot) + "\n";
						}
					}
					else
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType() == componentField.Reference.Target.GetType()))
						{
							count++;
							text += c.GetType().GetNiceName() + " - " + (c.Enabled ? "Enabled" : "Disabled") + " - " + GetSlotParentHierarchyString(c.Slot) + "\n";
						}
					}
					
					UpdateStatusText($"Found {count} matching components.");

					if (showDetails.Value.Value)
					{
						Slot textSlot = WizardSlot.LocalUserSpace.AddSlot("Search Text");
						UniversalImporter.SpawnText(textSlot, text, new color(1f, 1f, 1f, 0.5f), textSize: 12, canvasSize: new float2(640, 360)); ;
						textSlot.PositionInFrontOfUser();
					}
				};

				UI.Text("----------");

				var enableButton = UI.Button("Enable");
				enableButton.LocalPressed += (IButton button, ButtonEventData data) =>
				{

					if (!ValidateWizard()) return;

					int count = 0;
					if (ignoreGenericTypes.Value.Value == true)
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType().Name == componentField.Reference.Target.GetType().Name))
						{
							c.Enabled = true;
							count++;
						}
					}
					else
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType() == componentField.Reference.Target.GetType()))
						{
							c.Enabled = true;
							count++;
						}
					}

					UpdateStatusText($"Enabled {count} matching components.");
				};

				var disableButton = UI.Button("Disable");
				disableButton.LocalPressed += (IButton button, ButtonEventData data) =>
				{

					if (!ValidateWizard()) return;

					int count = 0;
					if (ignoreGenericTypes.Value.Value == true)
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType().Name == componentField.Reference.Target.GetType().Name))
						{
							c.Enabled = false;
							count++;
						}
					}
					else
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType() == componentField.Reference.Target.GetType()))
						{
							c.Enabled = false;
							count++;
						}
					}

					UpdateStatusText($"Disabled {count} matching components.");
				};

				UI.HorizontalElementWithLabel("Confirm Destroy:", 0.942f, () => UI.BooleanMemberEditor(confirmDestroy.Value));

				var destroyButton = UI.Button("Destroy");
				destroyButton.LocalPressed += (IButton button, ButtonEventData data) =>
				{

					if (!ValidateWizard()) return;

					if (confirmDestroy.Value.Value == false)
					{
						UpdateStatusText("You must confirm destroy!");
						return;
					}

					int count = 0;
					if (ignoreGenericTypes.Value.Value == true)
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType().Name == componentField.Reference.Target.GetType().Name))
						{
							c.RunSynchronously(() =>
							{
								c.Destroy();
							});
							count++;
						}
					}
					else
					{
						foreach (Component c in processingRoot.Reference.Target.GetComponentsInChildren((Component c) => c.GetType() == componentField.Reference.Target.GetType()))
						{
							c.RunSynchronously(() =>
							{
								c.Destroy();
							});
							count++;
						}
					}

					confirmDestroy.Value.Value = false;
					UpdateStatusText($"Destroyed {count} matching components.");
				};

				processingRoot.Reference.Value = WizardSlot.World.RootSlot.ReferenceID;

				UI.Text("Status:");
				statusText = UI.Text("---");

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
			}

			void Slot_OnPrepareDestroy(Slot slot) {
				_Wizard = null;
			}

			//void CleanupLogix(IButton button, ButtonEventData eventData) {
			//	UpdateStatusText("Cleaning up LogiX");
			//	WizardSlot.World.Coroutines.StartTask(async () => {
			//		int totalRemovedComponents = await OptimizeLogiX(processingRoot.Reference, removeLogixReferences.Value, removeLogixInterfaceProxies.Value);
			//		Msg($"Removed {totalRemovedComponents} components");
			//		UpdateStatusText($"Removed {totalRemovedComponents} components");
			//	});
			//}

			//public static async Task<int> OptimizeLogiX(Slot targetSlot, bool removeLogixReferences, bool removeLogixInterfaceProxies) {
			//	if (targetSlot == null) {
			//		return 0;
			//	}
			//	await new Updates(10);
			//	List<Component> componentsForRemoval = targetSlot.GetComponentsInChildren((Component targetComponent) => {
			//		//Collect all LogiXReference and LogixInterfaceProxies for deletion
			//		if (removeLogixReferences && targetComponent is LogixReference) {
			//			return true;
			//		}
			//		if (removeLogixInterfaceProxies && targetComponent is LogixInterfaceProxy) {
			//			return true;
			//		}
			//		return false;
			//	});

			//	foreach (Component targetComponent in componentsForRemoval) {
			//		targetComponent.Destroy();
			//	}
			//	return componentsForRemoval.Count;
			//}

			//void DestroyInterfaces(IButton button, ButtonEventData eventData) {
			//	WizardSlot.World.Coroutines.StartTask(async () => {
			//		List<LogixInterface> interfaces = WizardSlot.World.RootSlot.GetComponentsInChildren<LogixInterface>();
			//		if (interfaces == null) {
			//			return;
			//		}
			//		int interfacesCount = interfaces.Count;
			//		foreach (LogixInterface @interface in interfaces) {
			//			@interface.Slot.Destroy();
			//		}
			//		Msg($"Destroyed {interfacesCount} Interfaces");
			//		UpdateStatusText($"Destroyed {interfacesCount} Interfaces");
			//	});
			//}

			//void RemoveEmptyRefs(IButton button, ButtonEventData eventData) {
			//	//Search for Slots named Ref with no components and no children
			//	WizardSlot.World.Coroutines.StartTask(async () => {
			//		List<Slot> refSlots = processingRoot.Reference.Target.GetAllChildren();
			//		UpdateStatusText($"Searching {refSlots.Count()} Slots");
			//		await new Updates(10);
			//		var removalCount = 0;
			//		foreach (Slot @ref in refSlots) {
			//			try {
			//				if (!(String.IsNullOrEmpty(@ref.Name))) {
			//					if (@ref.Name.Contains("Ref") && @ref.ChildrenCount == 0 && @ref.ComponentCount == 0) {
			//						@ref.Destroy();
			//						removalCount++;
			//					}
			//				}
			//			} catch (Exception e) {
			//				Msg(e);
			//			}
			//		}
			//		UpdateStatusText($"Removed {removalCount} Empty Refs");
			//	});
			//}

			//void RemoveEmptyCasts(IButton button, ButtonEventData eventData) {
			//	//Search for Slots named Cast with no components and no children
			//	WizardSlot.World.Coroutines.StartTask(async () => {
			//		List<Slot> refSlots = processingRoot.Reference.Target.GetAllChildren();
			//		UpdateStatusText($"Searching {refSlots.Count()} Slots");
			//		await new Updates(10);
			//		var removalCount = 0;
			//		foreach (Slot @ref in refSlots) {
			//			try {
			//				if (!(String.IsNullOrEmpty(@ref.Name))) {
			//					if (@ref.Name.Contains("Cast") && @ref.ChildrenCount == 0 && @ref.ComponentCount == 0) {
			//						@ref.Destroy();
			//						removalCount++;
			//					}
			//				}
			//			} catch (Exception e) {
			//				Msg(e);
			//			}
			//		}
			//		UpdateStatusText($"Removed {removalCount} Empty Casts");
			//	});
			//}
		}
	}
}
