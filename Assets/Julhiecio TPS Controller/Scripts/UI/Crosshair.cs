using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using JUTPS.JUInputSystem;
using JUTPS.CameraSystems;
using JUTPS.WeaponSystem;
using JUTPS;
using UnityEngine.InputSystem;

namespace JUTPS.UI
{
    [AddComponentMenu("JU TPS/UI/Crosshair")]
    public class Crosshair : MonoBehaviour
    {
        public static Crosshair Instance;
        public static bool AimingOnTarget;
        public static bool AimingOnFriend;
        public static GameObject ObjectOnCrosshairPoint;

        private JUCameraController cameraController;
        private JUCharacterController player;

        [Header("Settings")]
        public float CrosshairSensibility = 6;
        public float CrosshairChangeSpeed = 4;
        private float SmoothedWeaponPrecision;

        [Header("Hide Settings")]
        public bool FollowMousePosition;
        public bool HideOnNoWeaponUsing;
        public bool HideOnAiming;
        public bool OnlyShowOnFireMode;

        [Header("Visual Settings")]
        public Image[] Crosshairs;

        private Image CrosshairCenterPoint;
        private Canvas ParentCanvas;
        [HideInInspector] public List<Vector3> CrosshairsStartPositions = new List<Vector3>();
        [HideInInspector] public Vector3 CrosshairStartScale;

        public bool ChangeColor = true;
        public bool FilterPlayer = true;
        public string[] TargetTags = new string[] { "Enemy", "Skin", "Vehicle", "Zombie", "Monster", "Destructible", "Shootable", "Player" };
        public string[] NoShootableTags = new string[] { "Friend", "Unshootable" };
        public Color NormalColor = Color.white, ShootableColor = Color.red, NonShootableColor = new Color(1, 1, 1, 0.3f);

        protected virtual void Start()
        {
            // Gán instance
            Instance = this;

            // Tìm Camera (Sử dụng API mới để tránh Warning)
            cameraController = FindAnyObjectByType<JUCameraController>();

            // THAY ĐỔI TẠI ĐÂY: Không tìm Player ngay lập tức vì có thể chưa Spawn
            // Chúng ta sẽ dời việc kiểm tra Player vào Update để script không bị lỗi Start

            ////if theres no player, theres nothing to 
            //var playerobject = GameObject.FindGameObjectWithTag("Player");
            //player = playerobject.GetComponent<JUCharacterController>();
            //if (player == null) return;

            ////Save Crosshairs start positions
            //CrosshairsStartPositions = GetCrosshairPositions(Crosshairs);

            ////Save Start Scale
            //CrosshairStartScale = Crosshairs[0].transform.localScale;

            // Lưu vị trí bắt đầu của các thanh Crosshair
            if (Crosshairs != null && Crosshairs.Length > 0)
            {
                CrosshairsStartPositions = GetCrosshairPositions(Crosshairs);
                CrosshairStartScale = Crosshairs[0].transform.localScale;
            }

            // Lấy các thành phần UI cơ bản 
            CrosshairCenterPoint = GetComponent<Image>();
            ParentCanvas = GetComponentInParent<Canvas>();
        }
        protected virtual void Update()
        {
            // KIỂM TRA AN TOÀN: Nếu chưa gán Player, thử tìm lại từ GameManager
            if (player == null)
            {
                // Sử dụng JUGameManager để lấy Player đã được Assign bởi MultiplayerCameraAssigner
                if (JUGameManager.PlayerController != null)
                {
                    player = JUGameManager.PlayerController;
                }
                else
                {
                    // Nếu vẫn chưa có Player (chưa Host), ẩn Crosshair và thoát
                    SetActiveCrosshair(false);
                    return;
                }
            }

            // Nếu Camera bị null (do spawn chậm), thử tìm lại
            if (cameraController == null)
            {
                cameraController = Object.FindAnyObjectByType<JUCameraController>();
            }

            // Cập nhật trạng thái đối tượng dưới tâm ngắm
            UpdateObjectOnCrosshairPoint();
            // Cập nhật màu sắc của tâm ngắm
            UpdateCrosshairColor();
            // Cập nhật vị trí và kích thước của tâm ngắm
            UpdateCrosshair();
        }

        // Cập nhật vị trí và kích thước của tâm ngắm
        protected virtual void UpdateCrosshair()
        {
            // Nếu không có thanh Crosshair nào, thoát
            if (Crosshairs.Length == 0) return;

            // Lấy vũ khí đang sử dụng
            Weapon WeaponInUse = (player.LeftHandWeapon == null) ? player.RightHandWeapon : player.LeftHandWeapon;
            SmoothedWeaponPrecision = GetWeaponPrecisionValue(SmoothedWeaponPrecision, WeaponInUse, CrosshairChangeSpeed);

            if (Crosshairs.Length > 1)
            {
                // Cập nhật màu sắc của tâm ngắm
                UpdateCrosshairColor();

                // Cập nhật vị trí và kích thước của tâm ngắm
                MoveTowardsCenter(Crosshairs, CrosshairsStartPositions, SmoothedWeaponPrecision);
            }
            else
            {
                // Cập nhật vị trí và kích thước của tâm ngắm
                ResizeCrosshair(Crosshairs[0], SmoothedWeaponPrecision);
            }

            // Ẩn/Hiện tâm ngắm dựa trên trạng thái bắn và ngắm
            if (OnlyShowOnFireMode)
            {
                // Chỉ hiển thị khi ở chế độ bắn
                SetActiveCrosshair(player.FiringMode && !player.IsAiming);
            }
            else
            {
                // Ẩn tâm ngắm khi không có vũ khí
                HideCrosshairOnNoWeaponUsing();
                // Ẩn tâm ngắm khi đang ngắm
                HideCrosshairOnAiming();
            }

            // Cập nhật vị trí của tâm ngắm theo con trỏ chuột
            if (FollowMousePosition && Mouse.current != null)
            {
                Vector2 movePos;
                Vector2 mousePos = Mouse.current.position.value;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(ParentCanvas.transform as RectTransform, mousePos, ParentCanvas.worldCamera, out movePos);

                Vector3 mousePosOnUi = ParentCanvas.transform.TransformPoint(movePos);

                // them offset z để tránh bị che khuất
                CrosshairCenterPoint.transform.position = mousePos;

                // Cập nhật vị trí của từng thanh Crosshair
                transform.position = mousePos;
            }
        }

        // Cập nhật màu sắc của tâm ngắm dựa trên đối tượng dưới tâm ngắm
        protected virtual void UpdateCrosshairColor()
        {
            if (!ChangeColor) return;

            Color color = GetCurrentCrosshairColor(ObjectOnCrosshairPoint);

            if (Crosshairs.Length > 1)
            {
                foreach (Image img in Crosshairs)
                {
                    img.color = color;
                }
            }
            else
            {
                Crosshairs[0].color = color;
            }
            CrosshairCenterPoint.color = color;
        }

        // Cập nhật đối tượng dưới tâm ngắm
        protected virtual void UpdateObjectOnCrosshairPoint()
        {
            if (cameraController == null)
            {
                ObjectOnCrosshairPoint = null;
                return;
            }
            //Debug.Log(ObjectOnCrosshairPoint);
            // Lấy đối tượng dưới tâm ngắm
            GetObjectOnCrosshairPoint(cameraController.mCamera, cameraController.CrosshairRaycastLayerMask, out ObjectOnCrosshairPoint);
            // Lọc đối tượng là chính người chơi
            if (ObjectOnCrosshairPoint != null && FilterPlayer)
            {
                if (ObjectOnCrosshairPoint.layer == 15)
                {
                    JUTPS.CharacterBrain.JUCharacterBrain controllerBrain = ObjectOnCrosshairPoint.GetComponentInParent<JUTPS.CharacterBrain.JUCharacterBrain>();
                    if (controllerBrain != null)
                    {
                        if (cameraController == controllerBrain.MyPivotCamera)
                        {
                            ObjectOnCrosshairPoint = null;
                        }
                    }
                }
            }
        }
        // Khi vô hiệu hóa script, đặt đối tượng dưới tâm ngắm về null
        private void OnDisable()
        {
            ObjectOnCrosshairPoint = null;
        }

        // Lấy màu sắc hiện tại của tâm ngắm dựa trên đối tượng dưới tâm ngắm
        public Color GetCurrentCrosshairColor(GameObject ObjectOnCrosshairPoint)
        {
            Color color = NormalColor;
            if (ObjectOnCrosshairPoint == null) return color;
            // Kiểm tra nếu đang ngắm vào đối tượng không thể bắn
            if (IsAimingOnNonShootableObject(ObjectOnCrosshairPoint, NoShootableTags)) color = NonShootableColor;
            // Kiểm tra nếu đang ngắm vào đối tượng có thể bắn
            if (IsAimingOnShootableObject(ObjectOnCrosshairPoint, TargetTags)) color = ShootableColor;

            return color;
        }
        // Lấy đối tượng dưới tâm ngắm
        public static void GetObjectOnCrosshairPoint(Camera camera, LayerMask CrosshairRaycastLayerMask, out GameObject ObjectOnMousePosition)
        {
            if (Mouse.current == null)
            {
                ObjectOnMousePosition = null;
                return;
            }

            ObjectOnMousePosition = null;

            // Tạo tia từ vị trí con trỏ chuột
            Ray MouseRay = camera.ScreenPointToRay(Mouse.current.position.value);
            RaycastHit hit;
            if (Physics.Raycast(MouseRay, out hit, 1000, CrosshairRaycastLayerMask))
            {
                ObjectOnMousePosition = hit.collider.gameObject;
            }
        }
        // Kiểm tra nếu đang ngắm vào đối tượng có thể bắn
        public static bool IsAimingOnShootableObject(GameObject ObjectOnMousePosition, string[] TargetList)
        {
            bool isAimingOnTarget = false;

            foreach (string tag in TargetList)
            {
                if (ObjectOnMousePosition.tag == tag) isAimingOnTarget = true;
            }

            return isAimingOnTarget;
        }
        // Kiểm tra nếu đang ngắm vào đối tượng không thể bắn
        public static bool IsAimingOnNonShootableObject(GameObject ObjectOnMousePosition, string[] FriendList)
        {
            bool isAimingOnFriend = false;

            foreach (string tag in FriendList)
            {
                if (ObjectOnMousePosition.tag == tag) isAimingOnFriend = true;
            }

            return isAimingOnFriend;
        }


        // Cập nhật vị trí của các thanh Crosshair
        public void MoveTowardsCenter(Image[] crosshairs, List<Vector3> crosshairStartPositions, float precision)
        {
            for (int i = 0; i < crosshairs.Length; i++)
            {
                Vector3 normal = crosshairs[i].transform.position - crosshairs[i].transform.parent.position;
                crosshairs[i].transform.localPosition = crosshairStartPositions[i] + normal.normalized * precision;
            }
        }
        // Cập nhật kích thước của tâm ngắm
        public void ResizeCrosshair(Image crosshair, float precision)
        {
            if (crosshair == null) return;

            float CurrentSize = CrosshairStartScale.x + precision * CrosshairSensibility;
            crosshair.transform.localScale = new Vector3(CurrentSize, CurrentSize, CurrentSize);
        }
        // Ẩn/Hiện tâm ngắm
        public void SetActiveCrosshair(bool enabled)
        {
            if (Crosshairs.Length < 2)
            {
                Crosshairs[0].enabled = enabled;
            }
            else
            {
                foreach (Image img in Crosshairs)
                {
                    img.enabled = enabled;
                    CrosshairCenterPoint.enabled = enabled;
                }
            }
        }
        protected void HideCrosshairOnNoWeaponUsing()
        {
            if (!HideOnNoWeaponUsing) return;
            SetActiveCrosshair((player.HoldableItemInUseRightHand || player.HoldableItemInUseLeftHand) ? true : false);
        }
        public void HideCrosshairOnAiming()
        {
            if (!HideOnAiming || (HideOnNoWeaponUsing && player.HoldableItemInUseRightHand == null)) return;
            SetActiveCrosshair(!player.IsAiming);
        }
        //  Lấy vị trí của các thanh Crosshair
        public List<Vector3> GetCrosshairPositions(Image[] crosshairs)
        {
            List<Vector3> crosshairPositions = new List<Vector3>();
            foreach (Image img in crosshairs)
            {
                crosshairPositions.Add(img.transform.localPosition);
            }
            return crosshairPositions;
        }
        // Lấy giá trị độ chính xác của vũ khí
        public static float GetWeaponPrecisionValue(float Current, Weapon WeaponInUse, float Speed = 8)
        {
            if (Instance == null)
            {
                Instance = FindAnyObjectByType<Crosshair>();
                return 0;
            }
            var precision = Mathf.Lerp(Current, Instance.CrosshairSensibility * 100 * (WeaponInUse ? WeaponInUse.ShotErrorProbability : 0), Speed * Time.deltaTime);
            return precision;
        }

    }

}