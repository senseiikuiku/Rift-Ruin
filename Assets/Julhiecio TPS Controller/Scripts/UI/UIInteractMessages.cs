using System.Collections;
using System.Collections.Generic;
using JUTPS.ActionScripts;
using JUTPS.InteractionSystem;
using UnityEngine;
using UnityEngine.UI;

namespace JUTPS.UI
{
    public class UIInteractMessages : MonoBehaviour
    {
        [Header("Item Pickup Message")]
        [SerializeField] private GameObject PickUpMessageObject;
        [SerializeField] private bool SetMessagePositionToItemPosition = true;
        [SerializeField] private Vector3 Offset;
        [SerializeField] private bool ShowItemNameOnText;
        [SerializeField] private Text WarningText;
        [SerializeField] private string PickUpLabelText = "[HOLD] TO PICK UP ";
        [Header("Interaction Message")]
        [SerializeField] private string InteractLabelText = "TO INTERACT";
        [Header("Cover Trigger Message")]
        [SerializeField] private string CoverLabelText = "TO COVER";
        CoverSystem.JUCoverController PlayerCover;

        private void Start()
        {
            //JUGameManager.PlayerController = JUGameManager.PlayerController;
            //if (JUGameManager.PlayerController.TryGetComponent(out CoverSystem.JUCoverController cover))
            //{
            //    PlayerCover = cover;
            //}

            // Đảm bảo tin nhắn ẩn đi khi mới vào game
            if (PickUpMessageObject != null) PickUpMessageObject.SetActive(false);
        }
        void Update()
        {

            // KIỂM TRA AN TOÀN: Nếu chưa có người chơi (chưa bấm Host), thoát ngay
            if (JUGameManager.PlayerController == null)
            {
                if (PickUpMessageObject != null && PickUpMessageObject.activeSelf)
                    PickUpMessageObject.SetActive(false);
                return;
            }

            // Tự động tìm CoverController nếu chưa có (dành cho Multiplayer)
            if (PlayerCover == null)
            {
                JUGameManager.PlayerController.TryGetComponent(out CoverSystem.JUCoverController cover);
                PlayerCover = cover;
            }

            if (PlayerCover != null)
            {
                if (PlayerCover.CurrentCoverTrigger != null && PlayerCover.IsCovering == false && PlayerCover.AutoMode == false)
                {
                    PickUpMessageObject.SetActive(true);
                    UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, PlayerCover.CurrentCoverTrigger.GetCoverWallClosestPoint(PlayerCover.transform.position) + PlayerCover.transform.up * PlayerCover.CurrentCoverTrigger.transform.localScale.y / 2, Offset);
                    if (WarningText)
                    {
                        WarningText.text = CoverLabelText;
                    }
                    return;
                }
                else
                {
                    // Chỉ tắt nếu không có tương tác khác bên dưới
                    // PickUpMessageObject.SetActive(false);
                }
            }

            // KIỂM TRA INVENTORY AN TOÀN
            if (JUGameManager.PlayerController.Inventory == null)
            {
                PickUpMessageObject.SetActive(false);
                gameObject.SetActive(false);
                return;
            }

            // >> Interaction Message
            if (JUGameManager.PlayerController.TryGetComponent<JUInteractionSystem>(out var interactionSystem))
            {
                var canInteract = interactionSystem.CanInteract(interactionSystem.NearestInteractable);
                if (interactionSystem.BlockInteractions)
                    canInteract = false;

                if (canInteract)
                {
                    PickUpMessageObject.SetActive(true);
                    UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, interactionSystem.NearestInteractable.SelfCenter, Offset);
                    if (WarningText)
                    {
                        if (interactionSystem.NearestInteractable is JUTPS.InteractionSystem.Interactables.JUGeneralInteractable)
                        {
                            WarningText.text = (interactionSystem.NearestInteractable as InteractionSystem.Interactables.JUGeneralInteractable).InteractMessage;
                        }
                        else
                        {
                            WarningText.text = InteractLabelText;
                        }
                    }

                    return;
                }
            }

            //// >> Item Message
            //PickUpMessageObject.SetActive(JUGameManager.PlayerController.Inventory.ItemToPickUp != null);

            //if (PickUpMessageObject.activeInHierarchy && SetMessagePositionToItemPosition)
            //{
            //    UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, JUGameManager.PlayerController.Inventory.ItemToPickUp.transform.position, Offset);
            //}

            //if (ShowItemNameOnText && WarningText && JUGameManager.PlayerController.Inventory.ItemToPickUp != null)
            //{
            //    WarningText.text = PickUpLabelText + JUGameManager.PlayerController.Inventory.ItemToPickUp.ItemName;
            //}

            // >> Item Message (Nhặt vật phẩm)
            bool hasItemToPickUp = JUGameManager.PlayerController.Inventory.ItemToPickUp != null;
            PickUpMessageObject.SetActive(hasItemToPickUp);

            if (hasItemToPickUp)
            {
                if (SetMessagePositionToItemPosition)
                {
                    UIElementToWorldPosition.SetUIWorldPosition(PickUpMessageObject, JUGameManager.PlayerController.Inventory.ItemToPickUp.transform.position, Offset);
                }

                if (ShowItemNameOnText && WarningText)
                {
                    WarningText.text = PickUpLabelText + JUGameManager.PlayerController.Inventory.ItemToPickUp.ItemName;
                }
            }
            else if (PlayerCover == null || PlayerCover.CurrentCoverTrigger == null)
            {
                // Nếu không nhặt đồ cũng không núp cover thì mới tắt hẳn UI
                PickUpMessageObject.SetActive(false);
            }
        }
    }
}
