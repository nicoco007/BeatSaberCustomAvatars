using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomAvatar
{
	public interface IAvatarFullBodyInput:IAvatarInput
	{
		PosRot LeftLegPosRot { get; }
		PosRot RightLegPosRot { get; }
		PosRot PelvisPosRot { get; }
	}
}
