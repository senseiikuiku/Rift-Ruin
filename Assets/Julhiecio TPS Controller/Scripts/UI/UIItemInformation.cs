using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using JUTPS.ItemSystem;
using JUTPS.WeaponSystem;

namespace JUTPS.InventorySystem.UI
{
    public class UIItemInformation : MonoBehaviour
    {
        private JUHoldableItem CurrentItem;
        private JUCharacterController Player;

        [Header("Essentials")]
        public Sprite EmptySprite;
        public Image Icon;
        public Text ItemName;
        public Text ItemQuantity;
        public GameObject BulletLabel;
        public Text BulletQuantity;
        public Image ItemHealth;
        void Start()
        {
            //Player = JUGameManager.PlayerController;
        }

        // Update is called once per frame

        // Xử lý cập nhật thông tin mục trong mỗi khung hình
        void Update()
        {
            if (Player == null)
            {
                Player = JUGameManager.PlayerController;
                return;
            }

            if (Player.Inventory == null) return;

            // Lấy mục hiện tại trong tay phải của người chơi
            CurrentItem = Player.HoldableItemInUseRightHand;

            // Cập nhật giao diện dựa trên mục hiện tại
            if (CurrentItem == null)
            {
                Icon.sprite = EmptySprite;
                BulletLabel.SetActive(false);
                ItemName.text = "Hand";
                ItemQuantity.text = "";
                ItemHealth.fillAmount = 1;
            }
            else
            {
                // Xử lý trường hợp vật phẩm là vũ khí
                if (CurrentItem is Weapon)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(true);
                    // Cập nhật số lượng đạn và thanh sức khỏe của vũ khí
                    BulletQuantity.text = ((Weapon)CurrentItem).BulletsAmounts + "/" + ((Weapon)CurrentItem).TotalBullets;
                    // Cập nhật thanh sức khỏe của vật phẩm dựa trên số đạn còn lại
                    ItemHealth.fillAmount = (float)((Weapon)CurrentItem).BulletsAmounts / (float)((Weapon)CurrentItem).BulletsPerMagazine;
                    return;
                }

                // Xử lý trường hợp vật phẩm có thể cầm ném được
                if (CurrentItem is JUHoldableItem || CurrentItem is ThrowableItem)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(false);
                    // Cập nhật thanh sức khỏe của vật phẩm dựa trên số lượng hiện tại
                    ItemHealth.fillAmount = (float)CurrentItem.ItemQuantity / (float)CurrentItem.MaxItemQuantity;
                }

                // Xử lý trường hợp vũ khí cận chiến

                if (CurrentItem is MeleeWeapon)
                {
                    Icon.sprite = CurrentItem.ItemIcon;
                    ItemName.text = CurrentItem.ItemName;
                    ItemQuantity.text = CurrentItem.ItemQuantity + "/" + CurrentItem.MaxItemQuantity;

                    BulletLabel.SetActive(false);
                    ItemHealth.fillAmount = (float)((MeleeWeapon)CurrentItem).MeleeWeaponHealth / 100;

                }
            }
        }
    }

}