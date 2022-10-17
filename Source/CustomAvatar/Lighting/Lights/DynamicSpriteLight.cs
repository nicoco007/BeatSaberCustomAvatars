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

using System;
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

        public void Init(SpriteLightWithId spriteLightWithId, float intensityMultiplier)
        {
            _light = GetComponent<Light>();
            _spriteRenderer = spriteLightWithId.GetField<SpriteRenderer, SpriteLightWithId>("_spriteRenderer");
            _spriteTransform = _spriteRenderer.transform;
            _intensityMultiplier = intensityMultiplier * GetSpriteIntensity(_spriteRenderer.sprite);
        }

        public void Start()
        {
            // this is a decent optimization as long as there are no moving sprite lights (I *think* that's currently the case)
            Vector3 position = _spriteTransform.position - kOrigin;
            _intensityMultiplier *= 1 / (1 + position.magnitude) * _spriteRenderer.bounds.size.x * _spriteRenderer.bounds.size.y;
            transform.rotation = Quaternion.LookRotation(-position);
        }

        public void Update()
        {
            float intensity = _intensityMultiplier * _spriteRenderer.color.a;

            _light.color = _spriteRenderer.color;
            _light.intensity = intensity;
            _light.enabled = intensity > 0.0001f;
        }

        private float GetSpriteIntensity(Sprite sprite)
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
