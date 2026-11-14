---
name: rb-dotween
description: How to use DOTween correctly in Road Builder Racer - setup, the standard UI and game-feel patterns, sequences, easing, lifetime and cleanup rules, timescale handling, pooling safety, and common pitfalls. Use whenever writing, reviewing or debugging any tween, UI transition, popup, counter, shake, punch, or sequence.
---

# DOTween rules

DOTween (Demigiant) is free; DOTween Pro is paid and optional. It is **not** an official UPM
package, so it is imported from the Asset Store or the official download page, then run
**Tools > Demigiant > DOTween Utility Panel > Setup DOTween**. Use `using DG.Tweening;`.
Keep it under `Assets/Plugins/Demigiant/` (excluded from the graph). Verify the installed
version supports TMP shortcuts; the free build may not, so use `CanvasGroup.DOFade` and
`DOTween.To(getter, setter, end, duration)` for TMP text properties.

## Where tweens live (architecture)

Tweens belong in the **Animation layer**, never in Managers or game rules. A View calls a small
animation helper (`UIAnim.PopIn(rt)`, `UIAnim.CountUp(label, from, to)`). Gameplay code raises
events; the view animates in response. Gameplay state must not depend on a tween completing
(a tween can be killed, so use events or state, not `OnComplete`, for logic).

## Boot once

```csharp
DOTween.Init(recycleAllByDefault: true, useSafeMode: true, LogBehaviour.ErrorsOnly)
        .SetCapacity(200, 50);   // tweens, sequences, avoids runtime growth
DOTween.defaultEaseType = Ease.OutCubic;
DOTween.defaultAutoKill = true;
```
Capacity is a tuning step: raise it if the Utility Panel logs a capacity warning.

## Golden rules

1. **Always link to a GameObject:** `.SetLink(gameObject)` (or `SetLink(go, LinkBehaviour.KillOnDestroy)`).
   Prevents "target destroyed" errors, the most common DOTween bug.
2. **Kill before re-tweening the same target:** `rt.DOKill(); rt.DOScale(...)`. Or use an
   `id` and `DOTween.Kill(id)`. Never stack tweens on one property.
3. **UI uses unscaled time:** `.SetUpdate(true)` for anything that must run while `Time.timeScale`
   is 0 (pause, game-over, popups). Gameplay-feel tweens use scaled time on purpose (slow-mo).
4. **Menus reset state on enable:** set the start values explicitly in `OnEnable`; otherwise a
   killed tween leaves the object half scaled/transparent.
5. **No allocations per frame:** do not create tweens in `Update`. Cache `Sequence`s that repeat
   and `Restart()` them, or create once per event. Avoid lambdas capturing in hot paths.
6. **Never loop with `-1` and forget it:** every infinite loop (pulse, idle bob) is owned by a
   component that kills it in `OnDisable`.
7. **Pooled objects:** `DOKill()` in the pool release, reset transform/colour on get.
8. **Scene change:** `DOTween.KillAll()` before loading a non-additive scene (linking covers
   most, this is the belt and braces). Do not call it for additive loads.
9. **Don't tween physics bodies' transform.** Tween via `Rigidbody2D.DOMove` only for kinematic
   bodies; the car and road are physics-driven, so animate **visual child objects** instead.
10. Use `SetEase` deliberately (see `rb-motion-design`); default linear is almost never right.

## Pattern library (put these in one static `UIAnim` helper)

```csharp
// Pop in: scale from 0.8 with overshoot + fade
public static Sequence PopIn(RectTransform rt, CanvasGroup cg, float t = 0.28f) {
    rt.DOKill(); cg.DOKill();
    rt.localScale = Vector3.one * 0.8f; cg.alpha = 0;
    return DOTween.Sequence().SetLink(rt.gameObject).SetUpdate(true)
        .Join(rt.DOScale(1f, t).SetEase(Ease.OutBack))
        .Join(cg.DOFade(1f, t * 0.6f));
}
// Button press: squash then release
public static Tween Press(Transform tr) {
    tr.DOKill(true);
    return tr.DOPunchScale(Vector3.one * -0.12f, 0.18f, 1, 0.5f)
             .SetLink(tr.gameObject).SetUpdate(true);
}
// Count-up number (score, coins) with TMP
public static Tween CountUp(TMP_Text label, int from, int to, float t = 0.8f) =>
    DOTween.To(() => from, v => { from = v; label.SetText("{0}", v); }, to, t)
           .SetEase(Ease.OutQuad).SetLink(label.gameObject).SetUpdate(true);
// Pulse for a warning (owned and killed by the caller)
public static Tween Pulse(Transform tr) =>
    tr.DOScale(1.12f, 0.35f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
      .SetLink(tr.gameObject).SetUpdate(true);
```
Other staples: screen transitions (slide + fade with `Sequence`), coin fly-to-counter (`DOPath`
or two-step `DOMove` with `OutQuad`, then punch the counter), camera shake (`DOShakePosition` on
a camera **child** rig), hit-stop (brief `DOTween.To` on `Time.timeScale` with `SetUpdate(true)`),
`SetDelay` + `SetRecyclable` for staggered list entries (30-60 ms stagger).

## Sequences

Use `Append` (after), `Join` (with), `Insert(time, t)`, `AppendCallback` (cosmetic callbacks only),
`AppendInterval`. Give a sequence an overall `SetEase` only when it is the whole motion.
Name complex ones with `SetId("popup_open")` so they can be killed and logged.

## Debugging

- "Target or field is missing/null": a tween outlived its object, so add `SetLink`.
- Tween does nothing: wrong object killed it, `timeScale` is 0 without `SetUpdate(true)`, or a
  prior tween left the property at the end value.
- Jump at start: `From()` or missing start reset. Set initial values explicitly.
- Open the **DOTween Inspector** (on the `[DOTween]` object in Play mode) to see live tweens
  and counts; a growing count means a leak.
- Profile: tweens are cheap; the cost is usually **Canvas rebuilds**. Tween `CanvasGroup`/
  `transform`, not layout-driven properties (`sizeDelta` inside a LayoutGroup).

## Alternatives (only if it matters)

For zero-allocation needs PrimeTween is a lighter option, but this project standardises on
DOTween so all code looks the same. Do not mix libraries.
