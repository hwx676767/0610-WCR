using System.Collections.Generic;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SitArea : MonoBehaviour
{
    private static readonly Dictionary<int, SitArea> AreasById = new Dictionary<int, SitArea>();

    [SerializeField]
    private int sitAreaId = 1;

    [SerializeField]
    private float sitYOffset = 6.9f;

    [SerializeField]
    private float extraInteractPadding = 0.5f;

    private readonly HashSet<NetworkIdentity> playersInside = new HashSet<NetworkIdentity>();

    private uint? occupantNetId;

    public int SitAreaId => sitAreaId;

    public bool IsOccupied => occupantNetId.HasValue;

    public Vector3 SitWorldPosition => transform.position + Vector3.up * sitYOffset;

    private void Awake()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
            collider.isTrigger = true;

        RegisterArea();
    }

    private void OnValidate()
    {
        RegisterArea();
    }

    private void OnDestroy()
    {
        if (AreasById.TryGetValue(sitAreaId, out SitArea existing) && existing == this)
            AreasById.Remove(sitAreaId);
    }

    private void RegisterArea()
    {
        if (sitAreaId <= 0)
            return;

        if (AreasById.TryGetValue(sitAreaId, out SitArea existing) && existing != null && existing != this)
            Debug.LogWarning($"[SitArea] 重复的 Sit Area Id: {sitAreaId}，对象 {name} 会覆盖 {existing.name}");

        AreasById[sitAreaId] = this;
    }

    public static bool TryGetArea(int id, out SitArea area)
    {
        return AreasById.TryGetValue(id, out area);
    }

    public bool ContainsPlayer(NetworkIdentity player)
    {
        return player != null && playersInside.Contains(player);
    }

    public bool TryOccupy(NetworkIdentity player)
    {
        if (player == null || IsOccupied)
            return false;

        if (!ContainsPlayer(player))
            return false;

        occupantNetId = player.netId;
        return true;
    }

    public void Release(NetworkIdentity player)
    {
        if (player == null)
            return;

        if (occupantNetId.HasValue && occupantNetId.Value == player.netId)
            occupantNetId = null;
    }

    public bool IsOccupiedBy(NetworkIdentity player)
    {
        return player != null && occupantNetId.HasValue && occupantNetId.Value == player.netId;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null)
            return;

        playersInside.Add(identity);

        PlayerController controller = identity.GetComponent<PlayerController>();
        if (controller != null)
            controller.NotifyEnterSitArea(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        NetworkIdentity identity = other.GetComponent<NetworkIdentity>();
        if (identity == null)
            return;

        playersInside.Remove(identity);

        PlayerController controller = identity.GetComponent<PlayerController>();
        if (controller != null)
            controller.NotifyExitSitArea(this);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = IsOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(SitWorldPosition, 0.25f);
        Gizmos.DrawLine(transform.position, SitWorldPosition);
    }
}
