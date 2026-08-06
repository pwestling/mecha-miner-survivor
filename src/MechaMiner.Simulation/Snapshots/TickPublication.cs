using System;
using System.Globalization;
using MechaMiner.Simulation.Events;

namespace MechaMiner.Simulation.Snapshots;

/// <summary>
/// The single result of one tick: the snapshot, the ordered domain batch, the ordered presentation batch,
/// and the publication diagnostics - or an explicit unpublished result for an invalidated tick.
/// </summary>
/// <remarks>
/// <para>
/// <c>docs/technical/20-simulation-core.md</c> § Tick transaction step 6: "Publish the committed state,
/// snapshot, events, and diagnostics as one tick result." That is why this is one type rather than three
/// return values: doc 115 § Component registry gives <c>CMP-SIM-003</c> both "double buffers and event
/// sequence" as its state and "immutable presentation snapshot and ordered event batch" as its output, so
/// a separate event publisher and snapshot publisher would give that state two writers and break
/// <c>TR-CTR-002</c>.
/// </para>
/// <para>
/// § Tick transaction also fixes the other half: "An exception or invariant failure before commit
/// invalidates the tick and ends the run through the safe technical-failure path; it never publishes a
/// partial state." An invalidated tick therefore yields a value whose <see cref="IsPublished"/> is false
/// and whose <see cref="Snapshot"/> is null - not a snapshot with some fields filled in.
/// </para>
/// <para>
/// <b>The buffer lease.</b> doc 115 § Cross-boundary contract registry: "Producers may reuse internal
/// buffers only after the consumer-facing snapshot/batch lifetime has ended under an explicit buffer-lease
/// contract." The two batch views span the publisher's own arrays, and the lease ends at
/// <c>SnapshotPublisher.ReleaseTick</c>. Both are <see cref="ReadOnlyMemory{T}"/> rather than arrays, so a
/// consumer holding one past the lease reads stale records but can never write through it and can never
/// mutate an authoritative batch.
/// </para>
/// </remarks>
public readonly struct TickPublication : IEquatable<TickPublication>
{
    private readonly PresentationSnapshot? _snapshot;
    private readonly ReadOnlyMemory<DomainEvent> _domainEvents;
    private readonly ReadOnlyMemory<PresentationEvent> _presentationEvents;
    private readonly long _tick;
    private readonly int _presentationSourceEventCount;
    private readonly string? _coalescingPolicyName;
    private readonly string? _invalidationReason;

    private TickPublication(
        PresentationSnapshot? snapshot,
        ReadOnlyMemory<DomainEvent> domainEvents,
        ReadOnlyMemory<PresentationEvent> presentationEvents,
        long tick,
        int presentationSourceEventCount,
        string coalescingPolicyName,
        string invalidationReason)
    {
        _snapshot = snapshot;
        _domainEvents = domainEvents;
        _presentationEvents = presentationEvents;
        _tick = tick;
        _presentationSourceEventCount = presentationSourceEventCount;
        _coalescingPolicyName = coalescingPolicyName;
        _invalidationReason = invalidationReason;
    }

    /// <summary>The tick this result describes.</summary>
    public long Tick => _tick;

    /// <summary>Whether the tick committed and published.</summary>
    public bool IsPublished => _snapshot is not null;

    /// <summary>The published snapshot, or <see langword="null"/> for an invalidated tick.</summary>
    public PresentationSnapshot? Snapshot => _snapshot;

    /// <summary>The published version, or unpublished for an invalidated tick.</summary>
    public SnapshotVersion Version => _snapshot?.Version ?? SnapshotVersion.Unpublished;

    /// <summary>The ordered domain batch. Never truncated and never dropped.</summary>
    public ReadOnlyMemory<DomainEvent> DomainEvents => _domainEvents;

    /// <summary>The ordered presentation batch, after the coalescing policy was applied.</summary>
    public ReadOnlyMemory<PresentationEvent> PresentationEvents => _presentationEvents;

    /// <summary>How many domain events the batch holds.</summary>
    public int DomainEventCount => _domainEvents.Length;

    /// <summary>How many records the presentation batch holds after coalescing.</summary>
    public int PresentationEventCount => _presentationEvents.Length;

    /// <summary>
    /// How many presentation events were emitted before coalescing, so a consumer can reconcile the merge.
    /// </summary>
    public int PresentationSourceEventCount => _presentationSourceEventCount;

    /// <summary>The name of the coalescing policy that produced the presentation batch.</summary>
    public string CoalescingPolicyName => _coalescingPolicyName ?? string.Empty;

    /// <summary>Why the tick published nothing, or empty when it published.</summary>
    public string InvalidationReason => _invalidationReason ?? string.Empty;

    /// <summary>Constructs the result of a committed tick. Called only by <c>SnapshotPublisher</c>.</summary>
    internal static TickPublication Published(
        PresentationSnapshot snapshot,
        ReadOnlyMemory<DomainEvent> domainEvents,
        ReadOnlyMemory<PresentationEvent> presentationEvents,
        int presentationSourceEventCount,
        string coalescingPolicyName)
    {
        return new TickPublication(
            snapshot,
            domainEvents,
            presentationEvents,
            snapshot.Tick,
            presentationSourceEventCount,
            coalescingPolicyName,
            string.Empty);
    }

    /// <summary>Constructs the result of an invalidated tick. Called only by <c>SnapshotPublisher</c>.</summary>
    internal static TickPublication Invalidated(long tick, string reason)
    {
        return new TickPublication(
            null,
            ReadOnlyMemory<DomainEvent>.Empty,
            ReadOnlyMemory<PresentationEvent>.Empty,
            tick,
            0,
            string.Empty,
            reason);
    }

    /// <summary>Compares two results by identity of the snapshot they name and by their batch views.</summary>
    public static bool operator ==(TickPublication left, TickPublication right)
    {
        return left.Equals(right);
    }

    /// <summary>Compares two results for inequality.</summary>
    public static bool operator !=(TickPublication left, TickPublication right)
    {
        return !left.Equals(right);
    }

    /// <inheritdoc/>
    public bool Equals(TickPublication other)
    {
        return ReferenceEquals(_snapshot, other._snapshot)
            && _tick == other._tick
            && _domainEvents.Equals(other._domainEvents)
            && _presentationEvents.Equals(other._presentationEvents)
            && _presentationSourceEventCount == other._presentationSourceEventCount
            && string.Equals(CoalescingPolicyName, other.CoalescingPolicyName, StringComparison.Ordinal)
            && string.Equals(InvalidationReason, other.InvalidationReason, StringComparison.Ordinal);
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is TickPublication other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return HashCode.Combine(
            _snapshot,
            _tick,
            _domainEvents.Length,
            _presentationEvents.Length,
            _presentationSourceEventCount,
            StringComparer.Ordinal.GetHashCode(CoalescingPolicyName),
            StringComparer.Ordinal.GetHashCode(InvalidationReason));
    }

    /// <summary>Renders the whole result as canonical invariant text, batches included.</summary>
    public string Render()
    {
        System.Text.StringBuilder builder = new();
        if (!IsPublished)
        {
            return "tick "
                + _tick.ToString(CultureInfo.InvariantCulture)
                + " published nothing: "
                + InvalidationReason
                + "\n";
        }

        builder.Append(_snapshot!.Render());
        builder
            .Append("domain-batch count=")
            .Append(_domainEvents.Length.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        ReadOnlySpan<DomainEvent> domain = _domainEvents.Span;
        for (int index = 0; index < domain.Length; index++)
        {
            builder.Append("  ").Append(domain[index].ToString()).Append('\n');
        }

        builder
            .Append("presentation-batch policy=")
            .Append(CoalescingPolicyName)
            .Append(" count=")
            .Append(_presentationEvents.Length.ToString(CultureInfo.InvariantCulture))
            .Append(" sources=")
            .Append(_presentationSourceEventCount.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        ReadOnlySpan<PresentationEvent> presentation = _presentationEvents.Span;
        for (int index = 0; index < presentation.Length; index++)
        {
            builder.Append("  ").Append(presentation[index].ToString()).Append('\n');
        }

        return builder.ToString();
    }

    /// <summary>Renders only the authoritative half: the snapshot and the domain batch.</summary>
    /// <remarks>
    /// <c>VER-SIM-006-007</c> needs to compare two runs that differ only in whether the presentation batch
    /// was discarded, which requires a rendering that deliberately excludes it.
    /// </remarks>
    public string RenderAuthoritative()
    {
        if (!IsPublished)
        {
            return "tick "
                + _tick.ToString(CultureInfo.InvariantCulture)
                + " published nothing: "
                + InvalidationReason
                + "\n";
        }

        System.Text.StringBuilder builder = new();
        builder.Append(_snapshot!.Render());
        builder
            .Append("domain-batch count=")
            .Append(_domainEvents.Length.ToString(CultureInfo.InvariantCulture))
            .Append('\n');

        ReadOnlySpan<DomainEvent> domain = _domainEvents.Span;
        for (int index = 0; index < domain.Length; index++)
        {
            builder.Append("  ").Append(domain[index].ToString()).Append('\n');
        }

        return builder.ToString();
    }
}
