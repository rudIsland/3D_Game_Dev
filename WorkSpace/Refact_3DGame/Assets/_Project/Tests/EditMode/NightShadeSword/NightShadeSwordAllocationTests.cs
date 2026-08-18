using System;
using NUnit.Framework;
using rudIsland.RPG3D.Characters.Enemies.NightShade;
using UnityEngine;

namespace rudIsland.RPG3D.Tests
{
    public sealed class NightShadeSwordAllocationTests
    {
        [Test]
        public void Update_대기상태반복호출은관리힙을할당하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 20f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            for (int index = 0; index < 10; index++)
            {
                machine.Update(0.016f);
            }

            GC.GetAllocatedBytesForCurrentThread();
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++)
            {
                machine.Update(0.016f);
            }

            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            Assert.That(allocatedBytes, Is.Zero);
        }

        [Test]
        public void Update_걷기상태반복호출은관리힙을할당하지않는다()
        {
            using var scope = new NightShadeSwordTestScope(
                new Vector3(0f, 0f, 4.5f));
            NightShadeSwordStateMachine machine =
                scope.CreateStateMachine(scope.CreateSettings());
            machine.Enable();
            machine.Update(0.016f);
            for (int index = 0; index < 10; index++)
            {
                machine.Update(0.016f);
            }

            GC.GetAllocatedBytesForCurrentThread();
            long bytesBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int index = 0; index < 1000; index++)
            {
                machine.Update(0.016f);
            }

            long allocatedBytes =
                GC.GetAllocatedBytesForCurrentThread() - bytesBefore;
            Assert.That(allocatedBytes, Is.Zero);
        }
    }
}
