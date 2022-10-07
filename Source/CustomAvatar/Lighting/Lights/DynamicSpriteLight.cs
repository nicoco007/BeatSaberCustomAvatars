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
        private float _spriteIntensity;

        [SerializeField]
        private Light _light;

        [SerializeField]
        private float _intensityMultiplier;

        public void Init(SpriteLightWithId spriteLightWithId, float intensityMultiplier)
        {
            _light = GetComponent<Light>();
            _spriteRenderer = spriteLightWithId.GetField<SpriteRenderer, SpriteLightWithId>("_spriteRenderer");
            _spriteTransform = _spriteRenderer.transform;
            _spriteIntensity = GetSpriteIntensity(_spriteRenderer.sprite);
            _intensityMultiplier = intensityMultiplier;
        }

        public void Update()
        {
            Vector3 position = _spriteTransform.position - kOrigin;
            float intensity = _spriteIntensity * _intensityMultiplier * _spriteRenderer.color.a * 1 / (1 + position.magnitude);

            _light.color = _spriteRenderer.color;
            _light.intensity = intensity;
            _light.enabled = intensity > 0.0001f;

            transform.rotation = Quaternion.LookRotation(-position);
        }

        private float GetSpriteIntensity(Sprite sprite)
        {
            if (kIntensities.TryGetValue(sprite, out float value))
            {
                return value;
            }

            Texture2D texture = sprite.texture;
            Rect rect = sprite.textureRect;

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
            int width = (int)rect.width;
            int height = (int)rect.height;

            for (int x = (int)rect.xMin; x < rect.xMax; x++)
            {
                for (int y = (int)rect.yMin; y < rect.yMax; y++)
                {
                    Color color = pixels[x + y * width];
                    total += (0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b) * color.a;
                }
            }

            if (texture != sprite.texture)
            {
                Destroy(texture);
            }

            value = total / (width * height);

            kIntensities.Add(sprite, value);

            return value;
        }
    }
}
