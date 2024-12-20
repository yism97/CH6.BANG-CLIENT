using UnityEngine;
using UnityEditor;

namespace Ironcow
{
    public static class AutoCollider
    {
        [MenuItem("Ironcow/Tool/AddBoxCollider #&c")]
        private static void AdjustBounds()
        {
            var selection = Selection.activeGameObject;
            Renderer mr = selection.GetComponentInChildren<MeshRenderer>();                 //MeshRenderer ��������
            if (mr == null) mr = selection.GetComponentInChildren<SkinnedMeshRenderer>();   //������ SkinnedMeshRenderer�� ��������
            if (mr == null) mr = selection.GetComponentInChildren<Renderer>();              //�׷��� ������ �������� ã�ƺ���
            if (mr == null) return;                                                         //�׷��� ������ �ƿ� ���°Ŵ� ����
            selection.transform.GetChild(0).position = new Vector3(0, -GetBottom(selection), 0);
            var box = selection.GetComponent<BoxCollider>();
            if (box == null) box = selection.AddComponent<BoxCollider>();
            box.center = mr.bounds.center;
            box.size = mr.bounds.size;
        }

        private static float GetBottom(GameObject gameObject)
        {
            var meshes = gameObject.GetComponentsInChildren<Renderer>();
            float min = 0;
            foreach (var mesh in meshes)
            {
                var bounds = mesh.bounds;
                var bottom = bounds.center.y - bounds.size.y / 2;
                min = Mathf.Min(min, bottom);
            }
            return min;
        }
    }
}