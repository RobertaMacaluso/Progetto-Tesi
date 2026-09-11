using System.Collections.Generic;
using UnityEngine;

public class VirtualCart : MonoBehaviour
{
    [Header("Contents")]
    [SerializeField] private List<VirtualArtifact> artifacts = new();

    // Artifacts currently inside the cart trigger.
    private readonly HashSet<VirtualArtifact>
        artifactsInsideTrigger = new();

    public IReadOnlyList<VirtualArtifact> Artifacts =>
        artifacts;


    private void OnTriggerEnter(Collider other)
    {
        VirtualArtifact artifact =
            other.GetComponentInParent<VirtualArtifact>();

        if (artifact == null)
            return;

        artifactsInsideTrigger.Add(artifact);

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            "entered cart trigger."
        );
    }


    private void OnTriggerExit(Collider other)
    {
        VirtualArtifact artifact =
            other.GetComponentInParent<VirtualArtifact>();

        if (artifact == null)
            return;

        artifactsInsideTrigger.Remove(artifact);

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            "exited cart trigger."
        );
    }


    public bool IsArtifactInsideTrigger(
        VirtualArtifact artifact)
    {
        return artifact != null &&
               artifactsInsideTrigger.Contains(artifact);
    }


    /// <summary>
    /// Adds the artifact to the cart after manipulation ends.
    /// </summary>
    public void AddArtifact(VirtualArtifact artifact)
    {
        if (artifact == null)
            return;

        if (artifacts.Contains(artifact))
            return;

        if (artifact.CurrentCart != null &&
            artifact.CurrentCart != this)
        {
            artifact.CurrentCart.RemoveArtifact(artifact);
        }

        artifacts.Add(artifact);

        // IMPORTANT:
        // No SetParent here.
        // The ParentConstraint handles the attachment.
        artifact.AttachToCart(this);

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            "added to cart."
        );
    }


    public void RemoveArtifact(VirtualArtifact artifact)
    {
        if (artifact == null)
            return;

        artifacts.Remove(artifact);

        Debug.Log(
            $"[VirtualCart] Artifact {artifact.ArtifactId} " +
            "removed from cart."
        );
    }


    public bool ContainsArtifact(
        VirtualArtifact artifact)
    {
        return artifacts.Contains(artifact);
    }
}