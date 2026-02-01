using System;
using UnityEngine;

namespace SuperliminalTAS.Components;

public sealed class PracticeModController : MonoBehaviour
{
#if LEGACY
    public PracticeModController(IntPtr ptr) : base(ptr) { }
#endif

    public static PracticeModController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void LateUpdate()
    {
    }
}
