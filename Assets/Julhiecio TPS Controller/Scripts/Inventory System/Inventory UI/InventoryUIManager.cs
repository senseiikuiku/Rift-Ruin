using JU.Editor;
using JUTPS.CameraSystems;
using JUTPS.ItemSystem;
using JUTPSEditor.JUHeader;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;


namespace JUTPS.InventorySystem.UI
{
    public class InventoryUIManager : MonoBehaviour
    {
        private bool _defaultMouseVisible;
        private bool _defaultMouseLock;

        private JUCharacterController _character;

        [JUHeader("Inventory Settings")]
        public GameObject InventoryScreen;
        [SerializeField] private JUInventory _targetInventory;
        public InventorySlotUI SlotPrefab;

        public bool ShowCursorWhenOpenInventory = true;
        public bool DisableMoveOnOpenInventory = true;
        [JUHeader("Slots Settings")]
        public bool FilterLeftHandItems = true;
        public int SlotsQuantity = -1;
        public GridLayoutGroup InventoryScrollViewContent;
        public List<InventorySlotUI> Slots = new List<InventorySlotUI>();
        public List<InventorySlotUI> EquipmentSlots = new List<InventorySlotUI>();
        private RectTransform inventoryScrollViewRectTransform;

        [JUHeader("Loot View Settings")]
        public bool IsLootView = false;
        public Transform Player;
        public string PlayerTag = "Player";
        public LayerMask CharacterLayer;
        public float CheckLootRadius = 1f;
        private JUInventory LootToGetItems;

        public bool IsOpened { get; private set; }

        // Hàm lấy và thiết lập TargetInventory
        public JUInventory TargetInventory
        {
            get => _targetInventory;
            set
            {
                _targetInventory = value;
                _character = null;

                if (_targetInventory)
                {
                    _character = _targetInventory.GetComponent<JUCharacterController>();
                }
            }
        }

        // Khởi tạo và thiết lập các thành phần cần thiết.
        void Awake()
        {
            if (InventoryScrollViewContent != null) inventoryScrollViewRectTransform = InventoryScrollViewContent.GetComponent<RectTransform>();

            if (IsLootView)
            {
                //Get player 
                if (Player == null && GameObject.FindGameObjectWithTag(PlayerTag) != null) { Player = GameObject.FindGameObjectWithTag(PlayerTag).transform; }
                return;
            }

            if (TargetInventory == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                // Kiểm tra xem có tìm thấy nhân vật không trước khi lấy Component
                if (playerObj != null)
                {
                    TargetInventory = playerObj.GetComponent<JUInventory>();
                }
                else
                {
                    // Nếu không thấy nhân vật (do chưa chọn Host/Client), tạm thời dừng lại
                    Debug.Log("Inventory Manager: Đang đợi nhân vật xuất hiện...");
                    // Bạn có thể chọn giữ script hoạt động hoặc tắt đi tùy ý
                    // gameObject.SetActive(false); 
                    return;
                }
            }

            // Nếu vẫn không có Inventory đích, tắt UI kho đồ
            if (TargetInventory == null)
            {
                gameObject.SetActive(false);
                return;
            }

            // Tạo và thiết lập các ô trong kho đồ
            if (Slots.Count == 0)
            {
                CreateInventorySlots(ref Slots, SlotsQuantity, TargetInventory, SlotPrefab, InventoryScrollViewContent);
                SetSlots(ref Slots, TargetInventory);
            }
            else
            {
                SetSlots(ref Slots, TargetInventory);
            }

            // Lên lịch làm mới kho đồ mỗi giây
            InvokeRepeating("RefreshInventory", 1, 1);
            if (Slots.Count > 0) { RenameAllSlotWithIndex(Slots); }

            // Lên lịch làm mới kho đồ mỗi giây
            InvokeRepeating(nameof(CheckCursorVisibility), 0.1f, 0.1f);
        }

        // Cập nhật trạng thái kho đồ mỗi khung hình.
        private void Update()
        {
            // Nếu chưa có Inventory đích, hãy thử tìm lại (dành cho lúc vừa mới bấm Host/Client)
            if (TargetInventory == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null)
                {
                    TargetInventory = playerObj.GetComponent<JUInventory>();
                    // Gọi lại Awake hoặc khởi tạo thủ công ở đây nếu cần
                    Awake();
                }
                return; // Thoát Update nếu vẫn chưa có Target
            }

            // Nếu không có màn hình kho đồ hoặc nội dung ScrollView, thoát
            if (InventoryScreen == null || InventoryScrollViewContent == null) return;

            // Cập nhật kích thước của ScrollView dựa trên số lượng ô
            inventoryScrollViewRectTransform.sizeDelta = new Vector3(inventoryScrollViewRectTransform.sizeDelta.x, Slots.Count * InventoryScrollViewContent.cellSize.y);
            if (IsLootView == true)
            {
                if (Player == null) return;

                //Check nearby inventories 
                Collider[] characters = Physics.OverlapBox(Player.position, new Vector3(CheckLootRadius, CheckLootRadius, CheckLootRadius), Quaternion.identity, CharacterLayer);

                //Debug.Log(characters.Length + " On Loot Sensor");
                if (characters.Length > 1)
                {
                    foreach (Collider col in characters)
                    {
                        if (col.gameObject != Player.gameObject && col.gameObject != null && LootToGetItems == null)
                        {
                            if (col.TryGetComponent(out JUInventory LootInventory))
                            {
                                if (LootInventory.IsALoot && LootInventory != LootToGetItems)
                                {
                                    LootToGetItems = LootInventory;
                                    TargetInventory = LootInventory;

                                    OpenInventory();
                                    CreateInventorySlots(ref Slots, LootInventory.AllItems.Length, LootInventory, SlotPrefab, InventoryScrollViewContent);
                                    SetActiveSlotsOptions(false);
                                }
                            }
                        }
                        if (col.gameObject == null)
                        {
                            ClearAllSlots();
                            LootToGetItems = null;
                            TargetInventory = null;
                            ExitInventory();
                        }
                    }
                }
                else
                {
                    if (InventoryScreen.activeInHierarchy)
                    {
                        ClearAllSlots();
                        LootToGetItems = null;
                        TargetInventory = null;
                        ExitInventory();
                    }
                }

                if (InventoryScreen.activeInHierarchy && LootToGetItems == null || TargetInventory == null) { ExitInventory(); }
                return;
            }

            if (TargetInventory && TargetInventory.PlayerInputs && TargetInventory.PlayerInputs.IsOpenInventoryTriggered && !JUPauseGame.IsPaused)
            {
                if (!InventoryScreen.activeInHierarchy) { OpenInventory(); } else { ExitInventory(); }
            }
            else if (TargetInventory && !TargetInventory.PlayerInputs)
                Debug.LogError($"The player inventory {TargetInventory.name} hasn't an input asset.");
        }

        // Mở kho đồ và thiết lập trạng thái con trỏ chuột.
        public void OpenInventory()
        {
            if (InventoryScreen == null) return;

            if (!JUEditor.IsGameFocused)
                return;

            InventoryScreen.SetActive(true);
            IsOpened = true;

            if (IsLootView) return;

            if (ShowCursorWhenOpenInventory)
            {
                JUCameraController.LockMouse(false, false);
            }

            JUPauseGame.AllowSetPaused = false;

            if (_character && DisableMoveOnOpenInventory)
            {
                _character.DisableLocomotion();
            }

        }

        // Đóng kho đồ và khôi phục trạng thái con trỏ chuột.
        public void ExitInventory()
        {
            if (InventoryScreen == null || !InventoryScreen.activeInHierarchy)
                return;

            if (!JUEditor.IsGameFocused)
                return;

            InventoryScreen.SetActive(false);
            IsOpened = false;

            if (IsLootView) return;
            JUCameraController.LockMouse(Lock: _defaultMouseLock, Hide: !_defaultMouseVisible);
            JUPauseGame.AllowSetPaused = true;

            if (_character && DisableMoveOnOpenInventory)
            {
                _character.enableMove();
            }
        }

        // Tạo các ô trong kho đồ dựa trên số lượng và loại vật phẩm.
        public static void CreateInventorySlots(ref List<InventorySlotUI> SlotsList, int SlotQuantity, JUInventory inventory, InventorySlotUI slotPrefab, GridLayoutGroup scrollViewContentGridLayout)
        {
            // Nếu SlotQuantity <= 0, tạo ô cho tất cả vật phẩm trong kho đồ
            if (SlotQuantity <= 0)
            {
                for (int i = 0; i < inventory.AllItems.Length; i++)
                {
                    var slot = InstantiateSlot(slotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, i, scrollViewContentGridLayout.transform);
                    SlotsList.Add(slot);
                }
            }
            else // Tạo số ô theo SlotQuantity
            {
                for (int i = 0; i < SlotQuantity; i++)
                {
                    var slot = InstantiateSlot(slotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, i, scrollViewContentGridLayout.transform);
                    SlotsList.Add(slot);
                }
            }
            RenameAllSlotWithIndex(SlotsList);
        }

        // Hàm hỗ trợ khởi tạo ô kho đồ.
        private static InventorySlotUI InstantiateSlot(InventorySlotUI SlotPrefab, InventorySlotUI.ItemArePlacedIn PlacedIn, int IDToDraw, Transform parent)
        {
            InventorySlotUI slot = (InventorySlotUI)Instantiate(SlotPrefab, parent);
            slot.PlacedIn = PlacedIn;
            slot.ItemIDToDraw = IDToDraw;
            return slot;
        }

        // Đặt lại tên tất cả các ô trong danh sách theo chỉ số của chúng.
        private static void RenameAllSlotWithIndex(List<InventorySlotUI> SlotsList)
        {
            int i = 0;
            foreach (InventorySlotUI slot in SlotsList)
            {
                slot.gameObject.name = "Slot " + i;
                i++;
            }
        }

        // Tạo các ô kho đồ mà không cần tham chiếu đến kho đồ cụ thể.
        public static void CreateInventorySlots(int SlotQuantity, InventorySlotUI slotPrefab, GridLayoutGroup scrollViewContentGridLayout)
        {
            if (SlotQuantity <= 0) return;

            for (int i = 0; i < SlotQuantity; i++)
            {
                InventorySlotUI slot = (InventorySlotUI)Instantiate(slotPrefab, scrollViewContentGridLayout.transform);
                slot.ItemIDToDraw = -1;
            }
        }

        // Thiết lập các ô kho đồ để hiển thị vật phẩm từ kho đồ cụ thể.
        public static void SetSlots(ref List<InventorySlotUI> SlotsList, JUInventory inventory)
        {
            for (int i = 0; i < inventory.AllItems.Length; i++)
            {
                SlotsList[i].ItemIDToDraw = i;
                SlotsList[i].RefreshSlot();
            }
        }

        // Kích hoạt hoặc vô hiệu hóa tùy chọn trên tất cả các ô kho đồ.
        public void SetActiveSlotsOptions(bool enabled)
        {
            foreach (InventorySlotUI slot in Slots)
            {
                slot.HideOptions();
                slot.EnableOptionsPanel = enabled;
            }
        }

        // Làm mới tất cả các ô kho đồ để hiển thị vật phẩm đúng.
        public void RefreshAllSlots()
        {
            foreach (InventorySlotUI currentSlot in Slots)
            {
                currentSlot.RefreshSlot();
                //Delete duplicated slots
                foreach (InventorySlotUI slotToVerify in Slots)
                {
                    if ((currentSlot != slotToVerify && currentSlot.ItemIDToDraw == slotToVerify.ItemIDToDraw) || IsItemInEquipmentSlots(slotToVerify.ItemIDToDraw))
                    {
                        slotToVerify.ItemIDToDraw = -2;
                        slotToVerify.RefreshSlot();
                    }
                }
            }

            List<JUItem> NonDrawedItems = GetNonDrawedItems(TargetInventory.AllItems, Slots, FilterLeftHandItems);
            SetupNonDrawedItemsInSlots(NonDrawedItems, inventory: this);
        }

        // Kiểm tra xem vật phẩm có đang được trang bị trong các ô trang bị không.
        private bool IsItemInEquipmentSlots(int itemID)
        {
            foreach (InventorySlotUI slot in EquipmentSlots)
            {
                if (itemID == slot.ItemIDToDraw) return true;
            }
            return false;
        }

        // Thiết lập các vật phẩm chưa được hiển thị trong các ô kho đồ.
        public static void SetupNonDrawedItemsInSlots(List<JUItem> nonDrawedItems, InventoryUIManager inventory)
        {
            if (nonDrawedItems.Count == 0 || inventory == null || inventory.Slots.Count == 0) return;

            foreach (JUItem item in nonDrawedItems)
            {
                // GET EMPTY SLOT
                InventorySlotUI emptySlot = GetFirstEmptySlot(inventory.Slots);
                if (emptySlot == null || inventory.IsItemInEquipmentSlots(JUInventory.GetGlobalItemSwitchID(item, inventory.TargetInventory))) return;
                // EMPTY IS NO LONGER EMPTY
                emptySlot.ItemIDToDraw = JUInventory.GetGlobalItemSwitchID(item, inventory.TargetInventory);
                emptySlot.RefreshSlot();
                emptySlot.IsEmpty = false;
            }

        }

        // Lấy danh sách các vật phẩm chưa được hiển thị trong kho đồ.
        public static List<JUItem> GetNonDrawedItems(JUItem[] items, List<InventorySlotUI> slots, bool filterLeftHandItems)
        {
            List<JUItem> NonDrawed = items.ToList();

            foreach (JUItem item in items)
            {
                if (item is JUHoldableItem && filterLeftHandItems)
                {
                    if ((item as JUHoldableItem).IsLeftHandItem)
                    {
                        NonDrawed.Remove(item);
                    }
                    else
                    {
                        if (IsItemDrawingInSomeSlots(item, slots, filterLeftHandItems))
                        {
                            NonDrawed.Remove(item);
                        }
                    }
                }
                else
                {
                    if (IsItemDrawingInSomeSlots(item, slots, filterLeftHandItems))
                    {
                        NonDrawed.Remove(item);
                    }
                }

                //foreach(InventorySlotUI slot in slots)
                //{
                //    if (slot.CurrentSlotItem() == item) NonDrawed.Remove(item);
                //}
            }

            return NonDrawed;
        }

        // Kiểm tra xem vật phẩm có đang được hiển thị trong bất kỳ ô kho đồ nào không.
        public static bool IsItemDrawingInSomeSlots(JUItem item, List<InventorySlotUI> slots, bool filterLeftHandItems)
        {
            bool isdrawing = false;

            foreach (InventorySlotUI slot in slots.ToArray())
            {
                if (filterLeftHandItems)
                {
                    if (item is JUHoldableItem)
                    {
                        if ((item as JUHoldableItem).IsLeftHandItem == true)
                        {
                            return false;
                        }
                        else
                        {
                            if (item == slot.CurrentSlotItem()) return true;
                        }
                    }
                    else
                    {
                        if (item == slot.CurrentSlotItem()) return true;

                    }
                }
                else
                {
                    if (item == slot.CurrentSlotItem()) return true;
                }
            }

            return isdrawing;
        }

        // Lấy ô kho đồ trống đầu tiên trong danh sách.
        public static InventorySlotUI GetFirstEmptySlot(List<InventorySlotUI> slots)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].ItemIDToDraw < 0) return slots[i];
            }
            Debug.LogWarning("Cannot find an empty slot in the list");
            return null;
        }

        // Lấy tất cả các ô kho đồ con của đối tượng này.
        public List<InventorySlotUI> GetSlots()
        {
            List<InventorySlotUI> slots = new List<InventorySlotUI>();
            slots = gameObject.GetComponentsInChildren<InventorySlotUI>().ToList();
            return slots;
        }

        // Xóa tất cả các ô kho đồ hiện có.
        public void ClearAllSlots()
        {
            foreach (InventorySlotUI slot in Slots)
            {
                Destroy(slot.gameObject);
            }
            Slots.Clear();
        }

        // Làm trống tất cả các ô kho đồ trong danh sách.
        public static void EmptyAllSlots(List<InventorySlotUI> SlotList)
        {
            foreach (InventorySlotUI slot in SlotList)
            {
                slot.ItemIDToDraw = -2;
                slot.RefreshSlot();
            }
        }

        /*
        public void FilterSlots(ref List<InventorySlotUI> slotList, SlotGenerationMode ShowOnly = SlotGenerationMode.RightHandItemsAndNonHoldableItems)
        {
            switch (ShowOnly)
            {
                // >>> Do nothing
                case SlotGenerationMode.AllItems:

                    break;
                // >>> Remove Holdable Left Hand Items and normal items
                case SlotGenerationMode.RightHandItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot); 
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                        else
                        {
                            Destroy(slot.gameObject); slotList.Remove(slot); 
                            var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                        }
                    }
                    break;

                // >>> Remove Holdable Left Hand Items Only
                case SlotGenerationMode.RightHandItemsAndNonHoldableItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                    }
                    break;

                // >>> Remove Holdable Right Hand Items and normal items
                case SlotGenerationMode.LeftHandItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem == false)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                        else
                        {
                            Destroy(slot.gameObject); slotList.Remove(slot);
                            var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                        }
                    }
                    break;

                // >>> Remove Holdable Right Hand Items Only
                case SlotGenerationMode.LeftHandItemsAndNonHoldableItems:
                    foreach (InventorySlotUI slot in slotList.ToList())
                    {
                        if (slot.CurrentSlotItem() is HoldableItem)
                        {
                            if ((slot.CurrentSlotItem() as HoldableItem).IsLeftHandItem == false)
                            {
                                Destroy(slot.gameObject); slotList.Remove(slot);
                                var newSlot = InstantiateSlot(SlotPrefab, InventorySlotUI.ItemArePlacedIn.AllBody, -1, InventoryScrollViewContent.transform); slotList.Add(newSlot);
                            }
                        }
                    }
                    break;
            }

        }
        */

        // Lọc các ô kho đồ để loại bỏ vật phẩm cầm tay cho tay trái.
        public void FilterSlots(List<InventorySlotUI> slotList)
        {
            // >>> Remove Holdable Left Hand Items Only
            foreach (InventorySlotUI slot in slotList.ToList())
            {
                if (slot.CurrentSlotItem() is JUHoldableItem)
                {
                    if ((slot.CurrentSlotItem() as JUHoldableItem).IsLeftHandItem)
                    {
                        slot.ItemIDToDraw = -2;
                        slot.RefreshSlot();
                    }

                    //if (TargetInventory.HoldableItensLeftHand.ToList().Contains(slot.CurrentSlotItem()))
                    //{
                    //    slot.ItemIDToDraw = -2;
                    //    slot.RefreshSlot();
                    //}
                }
            }
        }


        // Di chuyển một mục trong danh sách từ chỉ số cũ sang chỉ số mới.
        public static void Move<T>(List<T> list, int oldIndex, int newIndex)
        {
            T item = list[oldIndex];
            list.RemoveAt(oldIndex);
            list.Insert(newIndex, item);
        }

        // Làm mới kho đồ, bao gồm lọc các ô nếu cần.
        public void RefreshInventory()
        {
            //if (InventoryScreen.activeInHierarchy == false) return;
            RefreshAllSlots();
            if (FilterLeftHandItems)
            {
                FilterSlots(Slots);
            }
        }

        // Kiểm tra và lưu trạng thái hiển thị con trỏ chuột.

        private void CheckCursorVisibility()
        {
            if (JUPauseGame.IsPaused || IsOpened)
                return;

#if UNITY_EDITOR
            if (!JUEditor.IsGameFocused)
                return;
#endif

            _defaultMouseVisible = Cursor.visible;
            _defaultMouseLock = Cursor.lockState != CursorLockMode.None;
        }
    }

}