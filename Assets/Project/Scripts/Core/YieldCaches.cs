using System.Collections.Generic;
using UnityEngine;

public static class YieldCaches
{
    private static readonly Dictionary<float, WaitForSeconds> _timeInterval = new Dictionary<float, WaitForSeconds>();

    public static readonly WaitForSeconds Wait010 = Get(0.10f);
    public static readonly WaitForSeconds Wait020 = Get(0.20f);
    public static readonly WaitForSeconds Wait025 = Get(0.25f);
    public static readonly WaitForSeconds Wait028 = Get(0.28f);
    public static readonly WaitForSeconds Wait030 = Get(0.30f);
    public static readonly WaitForSeconds Wait032 = Get(0.32f);
    public static readonly WaitForSeconds Wait045 = Get(0.45f);
    public static readonly WaitForSeconds Wait050 = Get(0.50f);
    public static readonly WaitForSeconds Wait060 = Get(0.60f);
    public static readonly WaitForSeconds Wait100 = Get(1.00f);

    public static WaitForSeconds Get(float seconds)
    {
        if (!_timeInterval.TryGetValue(seconds, out WaitForSeconds wait))
        {
            wait = new WaitForSeconds(seconds);
            _timeInterval[seconds] = wait;
        }
        return wait;
    }
}
