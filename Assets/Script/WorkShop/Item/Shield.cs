using UnityEngine;

public class Shield : Item
{
    public int Deffent = 10;
    
    public override void OnCollect(Player player)
    {
        base.OnCollect(player);
        Vector3 ShielddUp = new Vector3(0, 0, 0);
        itemcollider.enabled = false;
        transform.parent = player.LeftHand;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(ShielddUp);
        player.Deffent += Deffent;
    }
}