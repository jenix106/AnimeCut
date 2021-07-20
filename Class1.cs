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
                if (part != null && !part.isSliced && part.ripBreak)
                {
                    part.EnableCharJointBreakForce(0);
                    part.ragdoll.creature.Kill();
                    yield return new WaitForEndOfFrame();
                }
                else if (part != null && !part.isSliced && !part.ripBreak)
                {
                    part.ragdoll.creature.currentHealth -= 20f;
                    if (part.ragdoll.creature.currentHealth <= 0f) part.ragdoll.creature.Kill();
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
                    if (part.ragdoll.creature != Player.currentCreature && parts.Contains(part) == false)
                    {
                        parts.Add(part);
                    }
                }
            }
        }
    }
}
