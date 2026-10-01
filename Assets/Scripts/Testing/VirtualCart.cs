using System.Collections.Generic;
using UnityEngine;

public class VirtualCart : MonoBehaviour
{
    [Header("Runtime contents")]
    [SerializeField]
    private List<VirtualArtifact> artifacts = new();

    /*
     * Usiamo un contatore invece di un semplice HashSet perché
     * uno stesso reperto può possedere più collider.
     */
    private readonly Dictionary<VirtualArtifact, int>
        artifactTriggerCounts = new();

    public IReadOnlyList<VirtualArtifact> Artifacts =>
        artifacts;

    private void Awake()
    {
        /*
         * Il carrello deve iniziare vuoto.
         * I reperti vengono aggiunti durante l'esecuzione.
         */
        artifacts.Clear();
        artifactTriggerCounts.Clear();
    }

    private void OnTriggerEnter(Collider other)
    {
        VirtualArtifact artifact =
            other.GetComponentInParent<VirtualArtifact>();

        if (artifact == null)
        {
            return;
        }

        artifactTriggerCounts.TryGetValue(
            artifact,
            out int currentCount
        );

        artifactTriggerCounts[artifact] =
            currentCount + 1;

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            $"entered cart trigger. Colliders inside: " +
            $"{artifactTriggerCounts[artifact]}."
        );
    }

    private void OnTriggerExit(Collider other)
    {
        VirtualArtifact artifact =
            other.GetComponentInParent<VirtualArtifact>();

        if (artifact == null)
        {
            return;
        }

        if (!artifactTriggerCounts.TryGetValue(
                artifact,
                out int currentCount))
        {
            return;
        }

        currentCount--;

        if (currentCount <= 0)
        {
            artifactTriggerCounts.Remove(artifact);

            Debug.Log(
                $"[VirtualCart] Artifact {artifact.ArtifactId} " +
                "completely exited cart trigger."
            );
        }
        else
        {
            artifactTriggerCounts[artifact] =
                currentCount;

            Debug.Log(
                $"[VirtualCart] Artifact {artifact.ArtifactId} " +
                $"partially exited cart trigger. " +
                $"Colliders still inside: {currentCount}."
            );
        }
    }

    public bool IsArtifactInsideTrigger(
        VirtualArtifact artifact)
    {
        if (artifact == null)
        {
            return false;
        }

        return artifactTriggerCounts.TryGetValue(
                   artifact,
                   out int count) &&
               count > 0;
    }

    public void AddArtifact(
        VirtualArtifact artifact)
    {
        if (artifact == null)
        {
            return;
        }

        if (artifact.CurrentCart != null &&
            artifact.CurrentCart != this)
        {
            artifact.CurrentCart.RemoveArtifact(
                artifact
            );
        }

        if (!artifacts.Contains(artifact))
        {
            artifacts.Add(artifact);
        }

        artifact.AttachToCart(this);

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            $"added to cart. Total: {artifacts.Count}."
        );
    }

    public void RemoveArtifact(
        VirtualArtifact artifact)
    {
        if (artifact == null)
        {
            return;
        }

        artifacts.Remove(artifact);

        /*
         * Non rimuoviamo qui artifactTriggerCounts:
         * quando viene preso, il reperto può essere ancora
         * fisicamente dentro il volume del carrello.
         * Il contatore verrà aggiornato da OnTriggerExit.
         */

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            $"removed from cart. Total: {artifacts.Count}."
        );
    }

    public bool ContainsArtifact(
        VirtualArtifact artifact)
    {
        return artifact != null &&
               artifacts.Contains(artifact);
    }

    /// <summary>
    /// Detaches all artifacts and clears the cart state.
    /// This will be used when resetting the experimental scene.
    /// </summary>
    public void ClearCart()
    {
        VirtualArtifact[] currentArtifacts =
            artifacts.ToArray();

        foreach (VirtualArtifact artifact
                 in currentArtifacts)
        {
            if (artifact != null &&
                artifact.CurrentCart == this)
            {
                artifact.DetachFromCart();
            }
        }

        artifacts.Clear();
        artifactTriggerCounts.Clear();

        Debug.Log(
            "[VirtualCart] Cart cleared."
        );
    }

    private void OnDisable()
    {
        artifactTriggerCounts.Clear();
    }
}