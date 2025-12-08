using System;
using System.Threading;

namespace YoungManGomoku_WebServer
{
	/*
	UID 생성 구조
	[ Timestamp bits ]  (상위 비트, 생성 시간 순으로 정렬되기 위함)
	[ ServerId bits   ]  (DB 저장이 필요하기에 서로 다른 서버에서 생성 시 유일성 보장)
	[ Counter bits    ]  (동시 생성 시 유일성 보장)

	조립된 UID는 마지막으로 난독화
	[ Obfuscation     ]  (Optional 난독화)
	 */

	// 인터널에는 다 이유가 있습니다. 서버 외 프로젝트에서 쓸 생각 마십쇼
	// 배포하면 다 뜯어질 클라를 뭘 어떻게 믿고 핵심 보안 기술을 내줌???
	internal sealed class UIDGenerator
	{
		private readonly UID32Generator uid32Generator;
		private readonly UID64Generator uid64Generator;

		// Server ID는 서버가 많아지면 DB에서 따로 테이블 파서 관리
		// 우리 프로젝트는 소규모라 1개만 굴러갈 것 같으니 그냥 디폴트 1로 땜빵
		public UIDGenerator(byte serverID = 1)
		{
			uid32Generator = new UID32Generator(serverID);
			ushort threadID = (ushort)(Thread.CurrentThread.ManagedThreadId & 0xFFFF);
			uid64Generator = new UID64Generator(serverID, threadID);
		}

		public uint GenerateUID32() => uid32Generator.GenerateUID();
		public ulong GenerateUID64() => uid64Generator.GenerateUID();
	}

	// 공통분모가 많은 32비트, 64비트 UID 제네레이터를 상속이나 인터페이스로 분할하지 않은 이유?
	// 비트 단위 연산이 들어가서 Generic이 부적합하고 멤버 함수의 반환형이 다르기 때문
	internal sealed class UID32Generator
	{
		// Server가 여러 개일 경우에도 Unique 보장
		private readonly byte serverID; // 0~127

		private readonly object lockObj = new object();
		private ushort counter = 0;
		private readonly long epoch;	// Time SEED

		public UID32Generator(byte serverID = 1)
		{
			this.serverID = (byte)(serverID & 0x7F); // 7bit
			epoch = new DateTime(2020, 1, 1).Ticks;
		}

		public uint GenerateUID()
		{
			uint raw;
			lock (lockObj)
			{
				long now = (DateTime.UtcNow.Ticks - epoch) / TimeSpan.TicksPerMillisecond;
				uint t = (uint)(now & 0x1FFFF); // 17bit
				uint c = counter++;             // 8bit 자동 overflow


				// [17bit: time (ms mod 131072 ≈ 2min window) ]
				// [7bit: serverId(0–127)]
				// [8bit: counter]
				raw = (t << 15) | ((uint)serverID << 8) | (c & 0xFF);
			}

			// 난독화 단계 – reversible하게 암호화
			return Obfuscate(raw);
		}

		// 인자로 들어온 값 난독화
		private uint Obfuscate(uint x)
		{
			// 임의의 값 XOR 처리, 난독화 후에도 생성 시간 정렬은 유효해야 하기에 시간 비트를 양쪽에 배치
			x ^= 0xA3C59AC3;
			x = (x << 7) | (x >> 25); // rotate-left 7
			x ^= 0x5F1ABB13;
			return x;
		}
	}

	internal sealed class UID64Generator
	{
		// Server가 여러 개일 경우에도 Unique 보장
		private readonly ushort serverID; // 0~1023 // 10bit

		// Process 내 UID 생성 단위, 보통 thread로 써도 됨
		private readonly ushort workerID; // 0~4095 // 12bit 
		private readonly object lockObj = new object();
		private uint counter = 0;
		private readonly long epoch;	// Time SEED

		public UID64Generator(ushort serverID, ushort workerID)
		{
			this.serverID = (ushort)(serverID & 0x03FF); // 10bit
			this.workerID = (ushort)(workerID & 0x0FFF); // 12bit
			epoch = new DateTime(2020, 1, 1).Ticks;
		}

		public ulong GenerateUID()
		{
			ulong raw;
			lock (lockObj)
			{
				long now = (DateTime.UtcNow.Ticks - epoch) / TimeSpan.TicksPerMillisecond;
				ulong t = ((ulong)now) & 0x3FFFFFFFFFFUL; // 42bit
				ulong c = counter++; // 32bit


				//[42bit: time (ms, 약 139년) ]
				//[10bit: serverId]
				//[12bit: process / workerId]
				//[32bit: counter]

				// Snowflake : 22 12 10 32 bit 구조로 비트 압축

				// Header 32bit 먼저 조립
				raw =
					(t << 22) |
					((ulong)serverID << 12) |
					workerID;

				// header | counter
				raw = (raw << 32) | c;

				// 이거랑 동일한 코드긴 한데... 가독성은 위가 더 나을지도, 숏코딩 하는 습관을 고쳐보자
				// raw = (t << 54) | ((ulong)serverID << 44) | ((ulong)workerID << 32) | c;
			}

			return Obfuscate(raw);
		}

		// 인자로 들어온 값 난독화
		private ulong Obfuscate(ulong x)
		{
			// 임의의 값 XOR 처리, 난독화 후에도 생성 시간 정렬은 유효해야 함
			x ^= 0xC3D2E1F0A5B4C3D2UL;
			x = (x << 13) | (x >> 51); // rotate-left 13
			x ^= 0x9E3779B97F4A7C15UL;
			return x;
		}
	}
}
