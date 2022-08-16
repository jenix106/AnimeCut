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
            foreach (RagdollPart part in parts)
            {
                if (part?.ragdoll?.creature?.gameObject?.activeSelf == true && part != null && !part.isSliced && part?.ragdoll?.creature != Player.currentCreature)
                {
                    //part.ragdoll.physicToggle = true;
                    if (part.sliceAllowed)
                    {
                        part.ragdoll.TrySlice(part);
                        if (part.data.sliceForceKill)
                            part.ragdoll.creature.Kill();
                        yield return null;
                    }
                    else if (!part.sliceAllowed && !part.ragdoll.creature.isKilled)
                    {
                        CollisionInstance instance = new CollisionInstance(new DamageStruct(DamageType.Slash, 20f));
                        instance.damageStruct.hitRagdollPart = part;
                        part.ragdoll.creature.Damage(instance);
                    }
                    //part.ragdoll.physicToggle = false;
                }
            }
            parts.Clear();
            yield break;
        }
        public void OnTriggerEnter(Collider c)
        {
            if (item.holder == null && c.GetComponentInParent<ColliderGroup>() != null)
            {
                ColliderGroup enemy = c.GetComponentInParent<ColliderGroup>();
                if (enemy?.collisionHandler?.ragdollPart != null && enemy?.collisionHandler?.ragdollPart?.ragdoll?.creature != Player.currentCreature)
                {
                    RagdollPart part = enemy.collisionHandler.ragdollPart;
                    part.gameObject.SetActive(true);
                    if (part.ragdoll.creature != Player.currentCreature && parts.Contains(part) == false)
                    {
                        parts.Add(part);
                    }
                }
            }
        }
    }
}
