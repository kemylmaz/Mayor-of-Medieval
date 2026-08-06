using System;
using System.Collections.Generic;
using MayorOfMedieval.Economy;
using UnityEngine;

namespace MayorOfMedieval.Building
{
    /// <summary>
    /// Recipe-driven workshop: consumes one unit from every input pile and produces one
    /// unit into the output pile on a timer. Covers the Mill (Grain + Water -> Bread),
    /// the Blacksmith (Stone -> Sword) and the Inn (Grain + Water + Bread -> Beer).
    /// </summary>
    public class ProductionBuilding : MonoBehaviour
    {
        [Serializable]
        public class Ingredient
        {
            public ResourceType type;
            public Stockpile pile;
            [Min(1)] public int amount = 1;
        }

        [Header("Recipe")]
        [SerializeField] private List<Ingredient> inputs = new List<Ingredient>();
        [SerializeField] private Stockpile output;
        [SerializeField] private float secondsPerCraft = 2.5f;

        [Header("Visual")]
        [Tooltip("Spun/animated only while actively crafting (mill sails, forge glow...).")]
        [SerializeField] private Transform spinner;
        [SerializeField] private Vector3 spinAxis = Vector3.forward;
        [SerializeField] private float spinSpeed = 90f;

        public bool IsCrafting { get; private set; }
        public ResourceType OutputType => output != null ? output.ResourceType : ResourceType.Gold;

        private float timer;

        /// <summary>Inputs a Producer worker should keep topped up.</summary>
        public IReadOnlyList<Ingredient> Inputs => inputs;

        private void Update()
        {
            IsCrafting = CanCraft();

            if (spinner != null && IsCrafting)
            {
                spinner.Rotate(spinAxis, spinSpeed * Time.deltaTime, Space.Self);
            }

            if (!IsCrafting)
            {
                timer = 0f;
                return;
            }

            timer += Time.deltaTime;
            if (timer < secondsPerCraft) return;
            timer = 0f;

            Craft();
        }

        private bool CanCraft()
        {
            if (output == null || output.IsFull) return false;
            if (inputs.Count == 0) return false;

            for (int i = 0; i < inputs.Count; i++)
            {
                Ingredient ing = inputs[i];
                if (ing == null || ing.pile == null) return false;
                if (ing.pile.Amount < ing.amount) return false;
            }
            return true;
        }

        private void Craft()
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                inputs[i].pile.Remove(inputs[i].amount);
            }
            output.Add(1);
        }

        /// <summary>Which ingredient pile most needs restocking, or null when all are full.</summary>
        public Ingredient NeediestInput()
        {
            Ingredient worst = null;
            float worstRatio = 1f;

            for (int i = 0; i < inputs.Count; i++)
            {
                Ingredient ing = inputs[i];
                if (ing == null || ing.pile == null || ing.pile.IsFull) continue;

                float ratio = ing.pile.Capacity <= 0 ? 1f : (float)ing.pile.Amount / ing.pile.Capacity;
                if (ratio >= worstRatio) continue;

                worstRatio = ratio;
                worst = ing;
            }
            return worst;
        }
    }
}
