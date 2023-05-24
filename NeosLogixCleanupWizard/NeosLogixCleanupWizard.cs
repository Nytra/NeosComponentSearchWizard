using System.Collections.Generic;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.UIX;
using BaseX;
using CodeX;
using HarmonyLib;
using System.CodeDom;

namespace ComponentWizard
{
	public class ComponentWizard : NeosMod {
		public override string Name => "Component Wizard";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/NeosComponentWizard";

		const string WIZARD_TITLE = "Component Search Wizard (Mod)";

		const string COMPONENT_NAME_TAG = "<color=green>";
		const string COMPONENT_NAME_TAG_END = "</color>";
		const string COMPONENT_ENABLED_TAG = "<color=yellow>";
		const string COMPONENT_ENABLED_TAG_END = "</color>";

		public override void OnEngineInit() {
			//Harmony harmony = new Harmony("owo.Nytra.ComponentWizard");
			Engine.Current.RunPostInit(AddMenuOption);
		} 
		void AddMenuOption() {
			DevCreateNewForm.AddAction("Editor", WIZARD_TITLE, (x) => LogixCleanupWizard.GetOrCreateWizard(x));
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
			readonly ValueField<string> nameField;
			readonly ValueField<bool> matchCase;
			readonly ValueField<bool> allowChanges;
			readonly ValueField<bool> searchNiceName;
			readonly ValueField<bool> conditionMode;

			readonly ReferenceMultiplexer<Component> results;

			readonly Button destroyButton;
			readonly Button searchButton;
			readonly Button enableButton;
			readonly Button disableButton;

			//enum ConditionMode
			//{
				//Any,
				//All
			//}

			static bool doingStuff = false;

			//readonly ValueField<ConditionMode> conditionMode;

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
					if (first)
					{
						str += slot.Name;
						first = false;
					}
					else
					{
						str += "/" + slot.Name;
					}
					
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

				//if (componentField.Reference.Target == null)
				//{
					//UpdateStatusText("No component provided!");
					//return false;
				//}

				if (doingStuff)
				{
					UpdateStatusText("Operations in progress! (Or the mod has crashed)");
					return false;
				}

				return true;
			}

			bool IsComponentMatch(Component c)
			{
				bool matchType, matchName;
				string compName, searchString;

				if (ignoreGenericTypes.Value.Value)
				{
					matchType = c.GetType().Name == componentField.Reference.Target?.GetType().Name;
				}
				else
				{
					matchType = c.GetType() == componentField.Reference.Target?.GetType();
				}

				compName = searchNiceName.Value.Value ? c.GetType().GetNiceName() : c.GetType().Name;
				compName = matchCase.Value.Value ? compName : compName.ToLower();

				searchString = matchCase.Value.Value ? nameField.Value.Value : nameField.Value.Value?.ToLower();

				matchName = nameField.Value.Value != null && nameField.Value.Value.Trim() != "" && compName.Contains(searchString);

				//switch (conditionMode.Value.Value)
				//{
				//	case ConditionMode.All:
				//		return matchType && matchName;
				//	case ConditionMode.Any:
				//		return matchType || matchName;
				//	default:
				//		return false;
				//}

				if (conditionMode.Value.Value)
				{
					return matchType && matchName;
				}
				else
				{
					return matchType || matchName;
				}
			}

			List<Component> GetSearchComponents()
			{
				return processingRoot.Reference.Target?.GetComponentsInChildren((Component c) => IsComponentMatch(c));
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
				canvasPanel.Panel.Title = WIZARD_TITLE;
				//canvasPanel.Canvas.Size.Value = new float2(400f, 390f);
				canvasPanel.Canvas.Size.Value = new float2(800f, 760f);

				Slot Data = WizardSlot.AddSlot("Data");
				this.processingRoot = Data.AddSlot("processingRoot").AttachComponent<ReferenceField<Slot>>();
				componentField = Data.AddSlot("componentField").AttachComponent<ReferenceField<Component>>();
				ignoreGenericTypes = Data.AddSlot("ignoreGenericTypes").AttachComponent<ValueField<bool>>();
				showDetails = Data.AddSlot("showDetails").AttachComponent<ValueField<bool>>();
				//showDetails.Value.Value = true;
				confirmDestroy = Data.AddSlot("confirmDestroy").AttachComponent<ValueField<bool>>();
				nameField = Data.AddSlot("nameField").AttachComponent<ValueField<string>>();
				conditionMode = Data.AddSlot("conditionMode").AttachComponent<ValueField<bool>>();
				matchCase = Data.AddSlot("matchCase").AttachComponent<ValueField<bool>>();
				allowChanges = Data.AddSlot("allowChanges").AttachComponent<ValueField<bool>>();
				searchNiceName = Data.AddSlot("searchNiceName").AttachComponent<ValueField<bool>>();

				UIBuilder UI = new UIBuilder(canvasPanel.Canvas);
				UI.Canvas.MarkDeveloper();
				UI.Canvas.AcceptPhysicalTouch.Value = false;

				//HorizontalLayout horizontalLayout = UI.HorizontalLayout(4f, childAlignment: Alignment.TopCenter);
				//horizontalLayout.ForceExpandHeight.Value = false;

				UI.SplitHorizontally(0.5f, out RectTransform left, out RectTransform right);

				left.OffsetMax.Value = new float2(-2f);
				right.OffsetMin.Value = new float2(2f);

				UI.NestInto(left);

				VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				UI.Style.MinHeight = 24f;
				UI.Style.PreferredHeight = 24f;
				UI.Style.PreferredWidth = 400f;
				UI.Style.MinWidth = 400f;

				UI.Text("Processing Root:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.Next("Root");
				UI.Current.AttachComponent<RefEditor>().Setup(processingRoot.Reference);

				UI.Spacer(24f);

				UI.Text("Component Type:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.Next("Component");
				UI.Current.AttachComponent<RefEditor>().Setup(componentField.Reference);

				UI.HorizontalElementWithLabel("Ignore Generic Type Arguments:", 0.942f, () => UI.BooleanMemberEditor(ignoreGenericTypes.Value));

				UI.Spacer(24f);

				UI.Text("Name Contains:").HorizontalAlign.Value = TextHorizontalAlignment.Left;
				UI.TextField().Text.Content.OnValueChange += (field) => nameField.Value.Value = field.Value;

				UI.HorizontalElementWithLabel("Match Case:", 0.942f, () => UI.BooleanMemberEditor(matchCase.Value));
				UI.HorizontalElementWithLabel("Search Full Component Name:", 0.942f, () => UI.BooleanMemberEditor(searchNiceName.Value));

				UI.Spacer(24f);

				UI.HorizontalElementWithLabel("Condition Mode (True: AND, False: OR):", 0.942f, () => UI.BooleanMemberEditor(conditionMode.Value));

				UI.Spacer(24f);

				//UI.Text("Condition Mode:").HorizontalAlign.Value = TextHorizontalAlignment.Left;

				//var enumSlot = UI.Next("Enum");
				//UI.NestInto(enumSlot);
				//UI.EnumMemberEditor(conditionMode.Value);
				//UI.NestOut();

				//UI.Text("");
				UI.HorizontalElementWithLabel("Spawn Detail Text:", 0.942f, () => UI.BooleanMemberEditor(showDetails.Value));

				//testButton = UI.Button("Make an explode owo");
				//testButton.LocalPressed += (IButton button, ButtonEventData data) => 
				//{
				//	Slot explodeSlot = WizardSlot.World.RootSlot.AddSlot("explode");
				//	explodeSlot.AttachComponent<ViolentAprilFoolsExplosion>();
				//	explodeSlot.Destroy();
				//};

				//UI.Text("----------");

				searchButton = UI.Button("Search");
				searchButton.LocalPressed += SearchPressed;

				UI.Text("----------");
				//UI.Text("");

				UI.HorizontalElementWithLabel("Allow Changes:", 0.942f, () => UI.BooleanMemberEditor(allowChanges.Value));

				enableButton = UI.Button("Enable");
				enableButton.LocalPressed += EnablePressed;

				disableButton = UI.Button("Disable");
				disableButton.LocalPressed += DisablePressed;

				UI.Spacer(24f);

				UI.HorizontalElementWithLabel("Confirm Destroy:", 0.942f, () => UI.BooleanMemberEditor(confirmDestroy.Value));

				destroyButton = UI.Button("Destroy");
				destroyButton.LocalPressed += DestroyPressed;

				processingRoot.Reference.Value = WizardSlot.World.RootSlot.ReferenceID;

				UI.Spacer(24f);

				UI.Text("Status:");
				statusText = UI.Text("---");

				results = Data.AddSlot("referenceMultiplexer").AttachComponent<ReferenceMultiplexer<Component>>();

				UI.NestInto(right);
				UI.ScrollArea();
				UI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);

				SyncMemberEditorBuilder.Build(results.References, "MatchingComponents", null, UI);

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);
			}

			void Slot_OnPrepareDestroy(Slot slot) {
				_Wizard = null;
			}

			void SearchPressed(IButton button, ButtonEventData eventData)
			{
				if (!ValidateWizard()) return;

				doingStuff = true;
				searchButton.Enabled = false;

				results.References.Clear();

				int count = 0;
				string text = "";
				if (ignoreGenericTypes.Value.Value == true)
				{
					foreach (Component c in GetSearchComponents())
					{
						count++;
						text += COMPONENT_NAME_TAG + c.GetType().GetNiceName() + COMPONENT_NAME_TAG_END + " - " + COMPONENT_ENABLED_TAG + (c.Enabled ? "Enabled" : "Disabled") + COMPONENT_ENABLED_TAG_END + " - " + GetSlotParentHierarchyString(c.Slot) + "\n";
						results.References.Add(c);
					}
				}
				else
				{
					foreach (Component c in GetSearchComponents())
					{
						count++;
						text += COMPONENT_NAME_TAG + c.GetType().GetNiceName() + COMPONENT_NAME_TAG_END + " - " + COMPONENT_ENABLED_TAG + (c.Enabled ? "Enabled" : "Disabled") + COMPONENT_ENABLED_TAG_END + " - " + GetSlotParentHierarchyString(c.Slot) + "\n";
						results.References.Add(c);
					}
				}

				UpdateStatusText($"Found {count} matching components.");

				if (showDetails.Value.Value && count > 0)
				{
					Slot textSlot = WizardSlot.LocalUserSpace.AddSlot("Search Text");
					UniversalImporter.SpawnText(textSlot, text, new color(1f, 1f, 1f, 0.5f), textSize: 12, canvasSize: new float2(800, 400)); ;
					textSlot.PositionInFrontOfUser();
				}

				doingStuff = false;
				searchButton.Enabled = true;
			}

			void EnablePressed(IButton button, ButtonEventData eventData)
			{
				if (!ValidateWizard()) return;

				if (!allowChanges.Value.Value)
				{
					UpdateStatusText("You must allow changes!");
					return;
				}

				if (results.References.Count == 0)
				{
					UpdateStatusText("No search results to process!");
					return;
				}

				doingStuff = true;
				enableButton.Enabled = false;

				int count = 0;
				foreach (Component c in results.References)
				{
					if (c != null)
					{
						c.Enabled = true;
						count++;
					}
				}

				UpdateStatusText($"Enabled {count} matching components.");

				doingStuff = false;
				enableButton.Enabled = true;
			}

			void DisablePressed(IButton button, ButtonEventData eventData)
			{
				if (!ValidateWizard()) return;

				if (!allowChanges.Value.Value)
				{
					UpdateStatusText("You must allow changes!");
					return;
				}

				if (results.References.Count == 0)
				{
					UpdateStatusText("No search results to process!");
					return;
				}

				doingStuff = true;
				disableButton.Enabled = false;

				int count = 0;
				foreach (Component c in results.References)
				{
					if (c != null)
					{
						c.Enabled = false;
						count++;
					}
				}

				UpdateStatusText($"Disabled {count} matching components.");

				doingStuff = false;
				disableButton.Enabled = true;
			}

			void DestroyPressed(IButton button, ButtonEventData eventData)
			{
				if (!ValidateWizard()) return;

				if (!allowChanges.Value.Value)
				{
					UpdateStatusText("You must allow changes!");
					return;
				}

				if (!confirmDestroy.Value.Value)
				{
					UpdateStatusText("You must confirm destroy!");
					return;
				}

				if (results.References.Count == 0)
				{
					UpdateStatusText("No search results to process!");
					return;
				}

				doingStuff = true;
				destroyButton.Enabled = false;

				int count = 0;
				foreach (Component c in results.References)
				{
					if (c != null)
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
				results.References.Clear();

				doingStuff = false;
				destroyButton.Enabled = true;
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
