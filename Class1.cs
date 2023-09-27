using System.Collections.Generic;
using System.Collections;
using ThunderRoad;
using UnityEngine;

namespace AnimeCut
{
    public class AnimeCutModule : ItemModule
    {
        public override void OnItemLoaded(Item item)
        {
            base.OnItemLoaded(item);
            item.gameObject.AddComponent<AnimeCutComponent>();
        }
    }
    public class AnimeCutComponent : MonoBehaviour
    {
        Item item;
        List<RagdollPart> parts = new List<RagdollPart>();
        Dictionary<Breakable, float> breakables = new Dictionary<Breakable, float>();
        bool active = false;
        Damager pierce;
        Damager slash;
        GameObject blades;
        GameObject triggers;
        SpellTelekinesis telekinesis;
        public void Awake()
        {
            item = GetComponent<Item>();
            item.OnSnapEvent += Item_OnSnapEvent;
            item.OnHeldActionEvent += Item_OnHeldActionEvent;
            item.OnUngrabEvent += Item_OnUngrabEvent;
            item.OnGrabEvent += Item_OnGrabEvent;
            item.OnTelekinesisReleaseEvent += Item_OnTelekinesisReleaseEvent;
            item.OnTelekinesisGrabEvent += Item_OnTelekinesisGrabEvent;
            pierce = item.GetCustomReference("Pierce").gameObject.GetComponent<Damager>();
            slash = item.GetCustomReference("Slash").gameObject.GetComponent<Damager>();
            blades = item.GetCustomReference("Blades").gameObject;
            triggers = item.GetCustomReference("Triggers").gameObject;
            triggers.SetActive(false);
        }

        private void Item_OnTelekinesisReleaseEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            telekinesis = null;
            Deactivate();
        }

        private void Item_OnTelekinesisGrabEvent(Handle handle, SpellTelekinesis teleGrabber)
        {
            telekinesis = teleGrabber;
            Deactivate();
        }

        private void Item_OnGrabEvent(Handle handle, RagdollHand ragdollHand)
        {
            Deactivate();
        }

        private void Item_OnUngrabEvent(Handle handle, RagdollHand ragdollHand, bool throwing)
        {
            Deactivate();
        }

        private void Item_OnHeldActionEvent(RagdollHand ragdollHand, Handle handle, Interactable.Action action)
        {
            if (action == Interactable.Action.AlternateUseStart)
            {
                Activate();
            }
            else if (action == Interactable.Action.AlternateUseStop)
            {
                Deactivate();
            }
        }
        public void FixedUpdate()
        {
            if(telekinesis != null && telekinesis.spinMode && !active)
            {
                Activate();
            }
            else if (telekinesis != null && !telekinesis.spinMode && active)
            {
                Deactivate();
            }
        }
        public void Deactivate()
        {
            blades.SetActive(true);
            triggers.SetActive(false);
            active = false;
        }
        public void Activate()
        {
            blades.SetActive(false);
            triggers.SetActive(true);
            pierce.UnPenetrateAll();
            slash.UnPenetrateAll();
            active = true;
        }

        private void Item_OnSnapEvent(Holder holder)
        {
            if (parts != null) StartCoroutine(AnimeSlice());
            Deactivate();
        }
        public IEnumerator AnimeSlice()
        {
            foreach (Breakable breakable in breakables.Keys)
            {
                --breakable.hitsUntilBreak;
                if (breakable.canInstantaneouslyBreak)
                    breakable.hitsUntilBreak = 0;
                breakable.onTakeDamage?.Invoke(breakables[breakable]);
                if (breakable.IsBroken || breakable.hitsUntilBreak > 0)
                    continue;
                breakable.Break();
            }
            breakables.Clear();
            foreach (RagdollPart part in parts)
            {
                if (part?.ragdoll?.creature?.gameObject?.activeSelf == true && part != null && !part.isSliced && part?.ragdoll?.creature != Player.currentCreature)
                {
                    part.gameObject.SetActive(true);
                    CollisionInstance instance = new CollisionInstance(new DamageStruct(DamageType.Slash, 20))
                    {
                        targetCollider = part.colliderGroup.colliders[0],
                        targetColliderGroup = part.colliderGroup,
                        sourceCollider = item.colliderGroups[0].colliders[0],
                        sourceColliderGroup = item.colliderGroups[0],
                        casterHand = item.lastHandler.caster,
                        impactVelocity = item.physicBody.velocity,
                        contactPoint = part.transform.position,
                        contactNormal = -item.physicBody.velocity
                    };
                    instance.damageStruct.hitRagdollPart = part;
                    if(item.colliderGroups[0].imbue.energy > 0 && item.colliderGroups[0].imbue is Imbue imbue)
                    {
                        imbue.spellCastBase.OnImbueCollisionStart(instance);
                        yield return null;
                    }
                    if (part.sliceAllowed)
                    {
                        part.ragdoll.TrySlice(part);
                        if (part.data.sliceForceKill)
                            part.ragdoll.creature.Kill();
                        yield return null;
                    }
                    part.ragdoll.creature.Damage(instance);
                }
            }
            parts.Clear();
            yield break;
        }
        public void OnTriggerEnter(Collider c)
        {
            if (item.holder == null && c.GetComponentInParent<Breakable>() is Breakable breakable)
            {
                if (!breakables.ContainsKey(breakable) || (breakables.ContainsKey(breakable) && item.physicBody.velocity.sqrMagnitude > breakables[breakable]))
                {
                    breakables.Remove(breakable);
                    breakables.Add(breakable, item.physicBody.velocity.sqrMagnitude);
                }
            }
            if (item.holder == null && c.GetComponentInParent<ColliderGroup>() is ColliderGroup group && group.collisionHandler.isRagdollPart)
            {
                group.collisionHandler.ragdollPart.gameObject.SetActive(true);
                if (!parts.Contains(group.collisionHandler.ragdollPart))
                {
                    parts.Add(group.collisionHandler.ragdollPart);
                }
            }
        }
    }
}
