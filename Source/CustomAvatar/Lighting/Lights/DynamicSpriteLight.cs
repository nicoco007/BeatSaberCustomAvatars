//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

        [SerializeReference]
        private LightIntensityData _lightIntensityData;

        [SerializeField]
        private TubeBloomPrePassLight _tubeBloomPrePassLight;

        [SerializeField]
        private bool _hideIfAlphaOutOfRange;

        [SerializeField]
        private float _hideAlphaRangeMin;

        [SerializeField]
        private float _hideAlphaRangeMax;

        private float _calculatedIntensity;

        public void Init(SpriteLightWithId spriteLightWithId, TubeBloomPrePassLight tubeBloomPrePassLight, LightIntensityData lightIntensityData)
        {
            _light = GetComponent<Light>();
            _spriteRenderer = spriteLightWithId.GetField<SpriteRenderer, SpriteLightWithId>("_spriteRenderer");
            _spriteTransform = _spriteRenderer.transform;
            _lightIntensityData = lightIntensityData;
            _tubeBloomPrePassLight = tubeBloomPrePassLight;
            _hideIfAlphaOutOfRange = spriteLightWithId.GetField<bool, SpriteLightWithId>("_hideIfAlphaOutOfRange");
            _hideAlphaRangeMin = spriteLightWithId.GetField<float, SpriteLightWithId>("_hideAlphaRangeMin");
            _hideAlphaRangeMax = spriteLightWithId.GetField<float, SpriteLightWithId>("_hideAlphaRangeMax");
        }

        public void Start()
        {
            Sprite sprite = _spriteRenderer.sprite;

            // I don't think this is particularly correct but it looks better than trying to do width * height (seems to scale too much for larger sprites)
            _calculatedIntensity = _spriteTransform.TransformVector(sprite.rect.width / sprite.pixelsPerUnit, sprite.rect.height / sprite.pixelsPerUnit, 0).magnitude
                * GetSpriteIntensity(_spriteRenderer.sprite)
                * _lightIntensityData.spriteLight;

            if (_tubeBloomPrePassLight != null)
            {
                _calculatedIntensity += _tubeBloomPrePassLight.transform.TransformVector(_tubeBloomPrePassLight.width, _tubeBloomPrePassLight.length, 0).magnitude
                    * Mathf.Pow(_tubeBloomPrePassLight.bloomFogIntensityMultiplier, 0.1f)
                    * _lightIntensityData.tubeBloomPrePassLight;
            }
        }

        public void Update()
        {
            Color color = _spriteRenderer.color;
            Vector3 position = _spriteTransform.position - kOrigin;

            transform.rotation = Quaternion.LookRotation(-position);

            _light.color = color;
            // technically should be using position.sqrMagnitude but it doesn't look as good
            _light.intensity = _calculatedIntensity * Mathf.Min(_spriteRenderer.color.a, 1) / (1 + position.magnitude) * Mathf.Abs(Vector3.Dot(-position.normalized, _spriteTransform.forward));

            if (_hideIfAlphaOutOfRange)
            {
                _light.enabled = color.a >= _hideAlphaRangeMin && color.a <= _hideAlphaRangeMax;
            }
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
            }

            if (texture != sprite.texture)
            {
                Destroy(texture);
            }

            value = total / (sprite.rect.width * sprite.rect.height);

            kIntensities.Add(sprite, value);

            return value;
        }
    }
}
