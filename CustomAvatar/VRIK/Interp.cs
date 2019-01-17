using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000170 RID: 368
	public class Interp
	{
		// Token: 0x0600098B RID: 2443 RVA: 0x00043208 File Offset: 0x00041608
		public static float Float(float t, InterpolationMode mode)
		{
			float result;
			switch (mode)
			{
			case InterpolationMode.None:
				result = Interp.None(t, 0f, 1f);
				break;
			case InterpolationMode.InOutCubic:
				result = Interp.InOutCubic(t, 0f, 1f);
				break;
			case InterpolationMode.InOutQuintic:
				result = Interp.InOutQuintic(t, 0f, 1f);
				break;
			case InterpolationMode.InOutSine:
				result = Interp.InOutSine(t, 0f, 1f);
				break;
			case InterpolationMode.InQuintic:
				result = Interp.InQuintic(t, 0f, 1f);
				break;
			case InterpolationMode.InQuartic:
				result = Interp.InQuartic(t, 0f, 1f);
				break;
			case InterpolationMode.InCubic:
				result = Interp.InCubic(t, 0f, 1f);
				break;
			case InterpolationMode.InQuadratic:
				result = Interp.InQuadratic(t, 0f, 1f);
				break;
			case InterpolationMode.InElastic:
				result = Interp.OutElastic(t, 0f, 1f);
				break;
			case InterpolationMode.InElasticSmall:
				result = Interp.InElasticSmall(t, 0f, 1f);
				break;
			case InterpolationMode.InElasticBig:
				result = Interp.InElasticBig(t, 0f, 1f);
				break;
			case InterpolationMode.InSine:
				result = Interp.InSine(t, 0f, 1f);
				break;
			case InterpolationMode.InBack:
				result = Interp.InBack(t, 0f, 1f);
				break;
			case InterpolationMode.OutQuintic:
				result = Interp.OutQuintic(t, 0f, 1f);
				break;
			case InterpolationMode.OutQuartic:
				result = Interp.OutQuartic(t, 0f, 1f);
				break;
			case InterpolationMode.OutCubic:
				result = Interp.OutCubic(t, 0f, 1f);
				break;
			case InterpolationMode.OutInCubic:
				result = Interp.OutInCubic(t, 0f, 1f);
				break;
			case InterpolationMode.OutInQuartic:
				result = Interp.OutInCubic(t, 0f, 1f);
				break;
			case InterpolationMode.OutElastic:
				result = Interp.OutElastic(t, 0f, 1f);
				break;
			case InterpolationMode.OutElasticSmall:
				result = Interp.OutElasticSmall(t, 0f, 1f);
				break;
			case InterpolationMode.OutElasticBig:
				result = Interp.OutElasticBig(t, 0f, 1f);
				break;
			case InterpolationMode.OutSine:
				result = Interp.OutSine(t, 0f, 1f);
				break;
			case InterpolationMode.OutBack:
				result = Interp.OutBack(t, 0f, 1f);
				break;
			case InterpolationMode.OutBackCubic:
				result = Interp.OutBackCubic(t, 0f, 1f);
				break;
			case InterpolationMode.OutBackQuartic:
				result = Interp.OutBackQuartic(t, 0f, 1f);
				break;
			case InterpolationMode.BackInCubic:
				result = Interp.BackInCubic(t, 0f, 1f);
				break;
			case InterpolationMode.BackInQuartic:
				result = Interp.BackInQuartic(t, 0f, 1f);
				break;
			default:
				result = 0f;
				break;
			}
			return result;
		}

		// Token: 0x0600098C RID: 2444 RVA: 0x000434F0 File Offset: 0x000418F0
		public static Vector3 V3(Vector3 v1, Vector3 v2, float t, InterpolationMode mode)
		{
			float num = Interp.Float(t, mode);
			return (1f - num) * v1 + num * v2;
		}

		// Token: 0x0600098D RID: 2445 RVA: 0x0004351E File Offset: 0x0004191E
		public static float LerpValue(float value, float target, float increaseSpeed, float decreaseSpeed)
		{
			if (value == target)
			{
				return target;
			}
			if (value < target)
			{
				return Mathf.Clamp(value + Time.deltaTime * increaseSpeed, float.NegativeInfinity, target);
			}
			return Mathf.Clamp(value - Time.deltaTime * decreaseSpeed, target, float.PositiveInfinity);
		}

		// Token: 0x0600098E RID: 2446 RVA: 0x00043559 File Offset: 0x00041959
		private static float None(float t, float b, float c)
		{
			return b + c * t;
		}

		// Token: 0x0600098F RID: 2447 RVA: 0x00043560 File Offset: 0x00041960
		private static float InOutCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (-2f * num2 + 3f * num);
		}

		// Token: 0x06000990 RID: 2448 RVA: 0x00043588 File Offset: 0x00041988
		private static float InOutQuintic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (6f * num2 * num + -15f * num * num + 10f * num2);
		}

		// Token: 0x06000991 RID: 2449 RVA: 0x000435BC File Offset: 0x000419BC
		private static float InQuintic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (num2 * num);
		}

		// Token: 0x06000992 RID: 2450 RVA: 0x000435D8 File Offset: 0x000419D8
		private static float InQuartic(float t, float b, float c)
		{
			float num = t * t;
			return b + c * (num * num);
		}

		// Token: 0x06000993 RID: 2451 RVA: 0x000435F0 File Offset: 0x000419F0
		private static float InCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * num2;
		}

		// Token: 0x06000994 RID: 2452 RVA: 0x0004360C File Offset: 0x00041A0C
		private static float InQuadratic(float t, float b, float c)
		{
			float num = t * t;
			return b + c * num;
		}

		// Token: 0x06000995 RID: 2453 RVA: 0x00043624 File Offset: 0x00041A24
		private static float OutQuintic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (num2 * num + -5f * num * num + 10f * num2 + -10f * num + 5f * t);
		}

		// Token: 0x06000996 RID: 2454 RVA: 0x00043664 File Offset: 0x00041A64
		private static float OutQuartic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (-1f * num * num + 4f * num2 + -6f * num + 4f * t);
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x000436A0 File Offset: 0x00041AA0
		private static float OutCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (num2 + -3f * num + 3f * t);
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x000436CC File Offset: 0x00041ACC
		private static float OutInCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (4f * num2 + -6f * num + 3f * t);
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x000436FC File Offset: 0x00041AFC
		private static float OutInQuartic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (6f * num2 + -9f * num + 4f * t);
		}

		// Token: 0x0600099A RID: 2458 RVA: 0x0004372C File Offset: 0x00041B2C
		private static float BackInCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (4f * num2 + -3f * num);
		}

		// Token: 0x0600099B RID: 2459 RVA: 0x00043754 File Offset: 0x00041B54
		private static float BackInQuartic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (2f * num * num + 2f * num2 + -3f * num);
		}

		// Token: 0x0600099C RID: 2460 RVA: 0x00043788 File Offset: 0x00041B88
		private static float OutBackCubic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (4f * num2 + -9f * num + 6f * t);
		}

		// Token: 0x0600099D RID: 2461 RVA: 0x000437B8 File Offset: 0x00041BB8
		private static float OutBackQuartic(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (-2f * num * num + 10f * num2 + -15f * num + 8f * t);
		}

		// Token: 0x0600099E RID: 2462 RVA: 0x000437F4 File Offset: 0x00041BF4
		private static float OutElasticSmall(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (33f * num2 * num + -106f * num * num + 126f * num2 + -67f * num + 15f * t);
		}

		// Token: 0x0600099F RID: 2463 RVA: 0x00043838 File Offset: 0x00041C38
		private static float OutElasticBig(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (56f * num2 * num + -175f * num * num + 200f * num2 + -100f * num + 20f * t);
		}

		// Token: 0x060009A0 RID: 2464 RVA: 0x0004387C File Offset: 0x00041C7C
		private static float InElasticSmall(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (33f * num2 * num + -59f * num * num + 32f * num2 + -5f * num);
		}

		// Token: 0x060009A1 RID: 2465 RVA: 0x000438B8 File Offset: 0x00041CB8
		private static float InElasticBig(float t, float b, float c)
		{
			float num = t * t;
			float num2 = num * t;
			return b + c * (56f * num2 * num + -105f * num * num + 60f * num2 + -10f * num);
		}

		// Token: 0x060009A2 RID: 2466 RVA: 0x000438F4 File Offset: 0x00041CF4
		private static float InSine(float t, float b, float c)
		{
			c -= b;
			return -c * Mathf.Cos(t / 1f * 1.57079637f) + c + b;
		}

		// Token: 0x060009A3 RID: 2467 RVA: 0x00043914 File Offset: 0x00041D14
		private static float OutSine(float t, float b, float c)
		{
			c -= b;
			return c * Mathf.Sin(t / 1f * 1.57079637f) + b;
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x00043931 File Offset: 0x00041D31
		private static float InOutSine(float t, float b, float c)
		{
			c -= b;
			return -c / 2f * (Mathf.Cos(3.14159274f * t / 1f) - 1f) + b;
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x0004395C File Offset: 0x00041D5C
		private static float InElastic(float t, float b, float c)
		{
			c -= b;
			float num = 1f;
			float num2 = num * 0.3f;
			float num3 = 0f;
			if (t == 0f)
			{
				return b;
			}
			if ((t /= num) == 1f)
			{
				return b + c;
			}
			float num4;
			if (num3 == 0f || num3 < Mathf.Abs(c))
			{
				num3 = c;
				num4 = num2 / 4f;
			}
			else
			{
				num4 = num2 / 6.28318548f * Mathf.Asin(c / num3);
			}
			return -(num3 * Mathf.Pow(2f, 10f * (t -= 1f)) * Mathf.Sin((t * num - num4) * 6.28318548f / num2)) + b;
		}

		// Token: 0x060009A6 RID: 2470 RVA: 0x00043A14 File Offset: 0x00041E14
		private static float OutElastic(float t, float b, float c)
		{
			c -= b;
			float num = 1f;
			float num2 = num * 0.3f;
			float num3 = 0f;
			if (t == 0f)
			{
				return b;
			}
			if ((t /= num) == 1f)
			{
				return b + c;
			}
			float num4;
			if (num3 == 0f || num3 < Mathf.Abs(c))
			{
				num3 = c;
				num4 = num2 / 4f;
			}
			else
			{
				num4 = num2 / 6.28318548f * Mathf.Asin(c / num3);
			}
			return num3 * Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * num - num4) * 6.28318548f / num2) + c + b;
		}

		// Token: 0x060009A7 RID: 2471 RVA: 0x00043AC4 File Offset: 0x00041EC4
		private static float InBack(float t, float b, float c)
		{
			c -= b;
			t /= 1f;
			float num = 1.70158f;
			return c * t * t * ((num + 1f) * t - num) + b;
		}

		// Token: 0x060009A8 RID: 2472 RVA: 0x00043AF8 File Offset: 0x00041EF8
		private static float OutBack(float t, float b, float c)
		{
			float num = 1.70158f;
			c -= b;
			t = t / 1f - 1f;
			return c * (t * t * ((num + 1f) * t + num) + 1f) + b;
		}
	}
}
