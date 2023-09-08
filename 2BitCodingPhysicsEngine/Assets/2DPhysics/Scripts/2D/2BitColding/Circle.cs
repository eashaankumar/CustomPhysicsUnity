using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MyPhysics.TwoD.TwoBitCoding
{
    public class Circle : Shape
    {
        [SerializeField]
        bool isStatic;

        SpriteRenderer spriteRenderer;

        public override void OnCollision(Shape other)
        {

        }

        public override void RandomGenerate()
        {
            spriteRenderer.color = Color.HSVToRGB(Random.value, Random.value, Random.Range(0.5f, 1.0f));

            float radius = Random.Range(0.2f, 1.0f) * 1;
            float density = Random.Range(0.5f, 10f);
            float restitution = Random.value;
            string error = "";

            if (!Body.CreateCircleBody(radius, transform.position, density, isStatic, restitution, out body, out error))
            {
                Debug.LogError(error);
                Destroy(gameObject);
            }

            //body.rotation = Quaternion.AngleAxis(Random.value * 360, Vector3.forward);
            body.rotation = transform.rotation;
            transform.localScale = Vector2.one * body.radius * 2;
        }

        // Start is called before the first frame update
        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();

        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.Lerp(spriteRenderer.color, Color.black, 0.5f);
            Vector2 up = body.rotation * Vector2.up;
            Gizmos.DrawLine(body.position, body.position + up * body.radius);
        }

        private void OnDrawGizmosSelected()
        {
            AABB aabb = body.GetAABB();

            Gizmos.color = Color.black;
            Gizmos.DrawWireCube(body.position, aabb.max - aabb.min);
        }

        private void Update()
        {
            transform.position = body.position;
            transform.localScale = Vector2.one * body.radius * 2;
            transform.rotation = body.rotation;
        }
    }
}