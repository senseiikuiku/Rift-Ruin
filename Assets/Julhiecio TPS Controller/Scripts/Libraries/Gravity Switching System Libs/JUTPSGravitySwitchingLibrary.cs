using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.CharacterBrain;

namespace JUTPS.GravitySwitchSystem
{
    public class JUGravity
    {
        /// <summary>
        /// Mô phỏng trọng lực dạng điểm (như một hành tinh nhỏ).
        /// </summary>
        /// <param name="GravityCenterPosition">Vị trí tâm hút.</param>
        /// <param name="Radious">Bán kính ảnh hưởng.</param>
        /// <param name="GravityForce">Lực hút (giá trị âm là hút vào).</param>
        /// <param name="AlignRigidBodies">Có xoay vật thể hướng về tâm không.</param>
        public static void SimulateGravityPoint(Vector3 GravityCenterPosition, float Radious = 10, float GravityForce = -200, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlignForce = 35)
        {
            Vector3 gravityCenter = GravityCenterPosition;
            // Tìm tất cả các vật thể trong vùng hình cầu
            Collider[] colliders = Physics.OverlapSphere(gravityCenter, Radious);
            foreach (Collider hit in colliders)
            {
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> XỬ LÝ TRỌNG LỰC (GRAVITY)
                    float distance = Vector3.Distance(rb.position, gravityCenter);
                    // Tính toán cường độ hút dựa trên khối lượng và khoảng cách
                    float attractionIntensity = (rb.mass / (distance * Radious));
                    // Xác định hướng từ vật thể đến tâm
                    Vector3 gravityDirection = (rb.position - gravityCenter).normalized;
                    // Áp dụng lực hút vật lý vào Rigidbody
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);

                    // >>> XỬ LÝ CĂN CHỈNH HƯỚNG (ALIGN)
                    // Nếu vật thể ở đủ xa và cho phép căn chỉnh, nó sẽ xoay chân về phía tâm
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }

        // Phiên bản nạp chồng (Overload) của SimulateGravityPoint có trả về danh sách Collider phát hiện được
        public static void SimulateGravityPoint(Vector3 GravityCenterPosition, out Collider[] rblist, float Radious = 10, float GravityForce = -200, bool AlignRigidBodies = false, float DistanceToStopAligning = 5, float AlignForce = 35, string[] TagsToIgnore = null)
        {
            // Lấy vị trí tâm trọng lực
            Vector3 gravityCenter = GravityCenterPosition;
            // Lấy danh sách collider và gán cho biến out rblist
            Collider[] colliders = Physics.OverlapSphere(gravityCenter, Radious);
            rblist = colliders;

            // Duyệt qua từng collider, lấy Rigidbody và áp dụng điểm trọng lực
            foreach (Collider hit in colliders)
            {
                bool ignoreThis = false;
                // Bỏ qua các vật thể có Tag nằm trong danh sách loại trừ
                if (TagsToIgnore != null)
                {
                    foreach (string tag in TagsToIgnore)
                        if (hit.CompareTag(tag) || hit.transform.root.CompareTag(tag))// Dùng CompareTag sẽ tối ưu hiệu năng hơn hit.tag
                        {
                            ignoreThis = true;
                            break;
                        }
                }
                if (ignoreThis) continue; // Bỏ qua vật thể này và CHẠY TIẾP vật thể sau (không dùng return)
                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> TRỌNG LỰC
                    float distance = Vector3.Distance(rb.position, gravityCenter);
                    float attractionIntensity = (rb.mass / (distance * Radious));
                    Vector3 gravityDirection = (rb.position - gravityCenter).normalized;
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);

                    // >>> CĂN CHỈNH XOAY
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Mô phỏng trọng lực bên trong một vùng hình hộp (như căn phòng không trọng lực hoặc đi trên tường).
        /// </summary>
        public static void SimulateGravityBox(Vector3 BoxPosition, Vector3 BoxScale, Quaternion BoxOrientation, Vector3 GravityDirection, float GravityForce, bool AlignRigidBodies, float AlignForce, float DistanceToStopAligning, out Collider[] collider, string[] TagsToIgnore = null)
        {
            // Quét các vật thể nằm trong vùng hình hộp (Box)
            Collider[] colliders = Physics.OverlapBox(BoxPosition, BoxScale, BoxOrientation);
            collider = colliders;

            // Duyệt từng vật thể để áp dụng lực trọng lực tùy chỉnh
            foreach (Collider hit in colliders)
            {
                bool ignoreThis = false;
                // Kiểm tra loại trừ tag
                if (TagsToIgnore != null && TagsToIgnore.Length > 0)
                {
                    Rigidbody parentRB = hit.attachedRigidbody; // Lấy Rigidbody mà Collider này thuộc về
                    foreach (string tag in TagsToIgnore)
                        if (hit.CompareTag(tag))
                        {
                            Debug.Log("<color=red>Đã chặn: </color>" + hit.name + " có tag: " + tag);
                            ignoreThis = true;
                            break;
                        }
                }

                if (ignoreThis)
                {
                    Debug.Log("<color=red>Đã chặn: </color>" + hit.name + " thuộc về gốc: " + hit.transform.root.name);
                    continue;
                }// Chỉ bỏ qua vật thể có tag enemy, các vật thể khác vẫn bị hút bình thường

                Rigidbody rb = hit.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    // >>> TRỌNG LỰC
                    float distance = Vector3.Distance(rb.position, BoxPosition);
                    // Cường độ hút hằng số (vì đây là vùng hộp, lực thường đều nhau)
                    float attractionIntensity = (rb.mass / (distance * 1));
                    // Nghịch đảo hướng GravityDirection truyền vào để làm hướng hút
                    Vector3 gravityDirection = -GravityDirection;
                    rb.AddForce(gravityDirection * ((100 * GravityForce) * Time.deltaTime) * attractionIntensity);

                    // >>> CĂN CHỈNH XOAY
                    if (distance > DistanceToStopAligning && AlignRigidBodies)
                    {
                        rb.transform.rotation = Quaternion.Lerp(rb.transform.rotation,
                            Quaternion.FromToRotation(rb.transform.up, gravityDirection) * rb.transform.rotation, AlignForce * attractionIntensity * Time.deltaTime);
                    }
                }
            }
        }

        /// <summary>
        /// Căn chỉnh hướng "Up" cho nhân vật JUTPS dựa theo tâm trọng lực (Dùng cho hành tinh nhỏ).
        /// </summary>
        public static void AlignJUTPSCharacterUpOrientation(Vector3 GravityCenterPosition, Collider[] collidersReturnedBySimulation, float DistanceToAlign)
        {
            foreach (Collider hit in collidersReturnedBySimulation)
            {
                // Truy cập vào "bộ não" của nhân vật JUTPS
                JUCharacterBrain character = hit.GetComponent<JUCharacterBrain>();
                float distance = Vector3.Distance(hit.transform.position, GravityCenterPosition);

                // Nếu tìm thấy nhân vật và nằm trong khoảng cách cho phép
                if (character != null && distance < DistanceToAlign)
                {
                    // Gán hướng thẳng đứng của nhân vật trùng với hướng từ tâm tỏa ra ngoài
                    character.UpDirection = (hit.transform.position - GravityCenterPosition).normalized;
                }
            }
        }

        /// <summary>
        /// Căn chỉnh hướng "Up" cho nhân vật JUTPS theo một hướng cố định (Dùng cho đi bộ trên tường/trần).
        /// </summary>
        public static void AlignJUTPSCharacterUpOrientation(Collider[] collidersReturnedBySimulation, Vector3 UpOrientation)
        {
            foreach (Collider hit in collidersReturnedBySimulation)
            {
                JUCharacterBrain character = hit.GetComponent<JUCharacterBrain>();

                if (character != null)
                {
                    // Gán hướng đứng thẳng của nhân vật theo hướng UpOrientation chỉ định
                    character.UpDirection = UpOrientation;
                }
            }
        }
    }
}