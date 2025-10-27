using UnityEngine;

public interface ITetherable
{
    void OnAttachTether();
    void OnTetherPull(GameObject tether);
    void OnDetachTether(GameObject tether);
}
