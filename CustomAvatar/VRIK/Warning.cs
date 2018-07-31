using System;
using UnityEngine;

namespace AvatarScriptPack
{
	// Token: 0x02000178 RID: 376
	public static class Warning
	{
		// Token: 0x060009E1 RID: 2529 RVA: 0x0004444A File Offset: 0x0004284A
		public static void Log(string message, Warning.Logger logger, bool logInEditMode = false)
		{
			if (!logInEditMode && !Application.isPlaying)
			{
				return;
			}
			if (Warning.logged)
			{
				return;
			}
			if (logger != null)
			{
				logger(message);
			}
			Warning.logged = true;
		}

		// Token: 0x060009E2 RID: 2530 RVA: 0x0004447B File Offset: 0x0004287B
		public static void Log(string message, Transform context, bool logInEditMode = false)
		{
			if (!logInEditMode && !Application.isPlaying)
			{
				return;
			}
			if (Warning.logged)
			{
				return;
			}
			Debug.LogWarning(message, context);
			Warning.logged = true;
		}

		// Token: 0x04000A14 RID: 2580
		public static bool logged;

		// Token: 0x02000179 RID: 377
		// (Invoke) Token: 0x060009E4 RID: 2532
		public delegate void Logger(string message);
	}
}
