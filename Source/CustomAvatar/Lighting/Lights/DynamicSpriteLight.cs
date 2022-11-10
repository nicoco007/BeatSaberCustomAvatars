//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2022  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System.Collections.Generic;
using IPA.Utilities;
using UnityEngine;

namespace CustomAvatar.Lighting.Lights
{
    internal class DynamicSpriteLight : MonoBehaviour
    {
        private static readonly Vector3 kOrigin = new Vector3(0, 1.5f, 0);
        private static readonly Dictionary<Sprite, float> kIntensities = new Dictionary<Sprite, float>();

        [SerializeField]
        private SpriteRenderer _spriteRenderer;

        [SerializeField]
        private Transform _spriteTransform;

        [SerializeField]
        private Light _light;

        [SerializeField]
        private float _intensityMultiplier;

        [SerializeField]
        private TubeBloomPrePassLight _tubeBloomPrePassLight;

        private float _calculatedIntensity;

        public void Init(SpriteLightWithId spriteLightWithId, TubeBloomPrePassLight tubeBloomPrePassLight, float intensityMultiplier)
        {
            _light = GetComponent<Light>();
            _spriteRenderer = spriteLightWithId.GetField<SpriteRenderer, SpriteLightWithId>("_spriteRenderer");
            _spriteTransform = _spriteRenderer.transform;
            _intensityMultiplier = intensityMultiplier;
            _tubeBloomPrePassLight = tubeBloomPrePassLight;
        }

        public void Start()
        {
            Sprite sprite = _spriteRenderer.sprite;
            float intensity = _spriteTransform.TransformVector(Vector3.right * sprite.rect.width / sprite.pixelsPerUnit).magnitude * _spriteTransform.TransformVector(Vector3.up * sprite.rect.height / sprite.pixelsPerUnit).magnitude;

            if (_tubeBloomPrePassLight != null)
            {
                Vector3 size = _tubeBloomPrePassLight.transform.TransformVector(new Vector3(_tubeBloomPrePassLight.width, _tubeBloomPrePassLight.length, 0));
                intensity += size.x * size.y * _tubeBloomPrePassLight.bloomFogIntensityMultiplier;
            }

            _calculatedIntensity = _intensityMultiplier * intensity * GetSpriteIntensity(_spriteRenderer.sprite);
        }

        public void Update()
        {
            Vector3 position = _spriteTransform.position - kOrigin;
            transform.rotation = Quaternion.LookRotation(-position);

            _light.color = _spriteRenderer.color;
            _light.intensity = _calculatedIntensity * Mathf.Min(_spriteRenderer.color.a, 1) / (1 + position.magnitude) * Mathf.Abs(Vector3.Dot(-position.normalized, _spriteTransform.forward));
        }

        private static float GetSpriteIntensity(Sprite sprite)
        {
            if (kIntensities.TryGetValue(sprite, out float value))
            {
                return value;
            }

            Texture2D texture = sprite.texture;

            if (!texture.isReadable)
            {
                var renderTexture = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = renderTexture;
                Graphics.Blit(texture, renderTexture);
                texture = renderTexture.GetTexture2D();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            Color[] pixels = texture.GetPixels();
            float total = 0;
            int count = 0;

            if (sprite.packed)
            {
                var uv = UV.Parse(sprite.uv, sprite.triangles);

                for (int x = 0; x < texture.width; ++x)
                {
                    for (int y = 0; y < texture.height; ++y)
                    {
                        if (uv.ContainsPoint(new Vector2((float)x / texture.width, (float)y / texture.height)))
                        {
                            Color color = pixels[x + y * texture.width];
                            total += (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) * color.a;
                            ++count;
                        }
                    }
                }
            }
            else
            {
                Rect rect = sprite.textureRect;
                int width = (int)rect.width;

                for (int x = (int)rect.xMin; x < rect.xMax; x++)
                {
                    for (int y = (int)rect.yMin; y < rect.yMax; y++)
                    {
                        Color color = pixels[x + y * width];
                        total += (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) * color.a;
                    }
                }

                count = Mathf.RoundToInt(rect.width * rect.height);
            }

            if (texture != sprite.texture)
            {
                Destroy(texture);
            }

            value = total / count;

            kIntensities.Add(sprite, value);

            return value;
        }
    }
}
