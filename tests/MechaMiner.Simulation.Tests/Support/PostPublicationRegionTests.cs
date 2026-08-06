using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using MechaMiner.Simulation.Commands;
using MechaMiner.Simulation.Snapshots;
using MechaMiner.Tests.Support;
using NUnit.Framework;

namespace MechaMiner.Simulation.Tests.Support;

/// <summary>
/// Reads the compiled bodies of the two methods that publish authoritative state and fails if anything
/// after the publication could throw.
/// </summary>
/// <remarks>
/// <para>
/// Verification: supports <c>VER-SIM-004-013</c> and <c>VER-SIM-004-007</c>.
/// </para>
/// <para>
/// <c>TR-RUN-007</c> in <c>docs/technical/112-normative-requirement-index.md</c> § Foundation and runtime:
/// "A run technical failure preserves the existing profile and does not publish partial state." That is
/// stated without qualifying when the failure arose, so it binds a failure raised inside a commit as much as
/// one raised before it. <c>docs/technical/20-simulation-core.md</c> § Tick transaction supplies the
/// mechanism for the pre-commit half - a failure there "invalidates the tick and ends the run through the
/// safe technical-failure path" - and § Mid-commit invalidation supplies it for the half from commit to
/// published snapshot. That clause also states where it stops: "the guarantee ends where invalidation does,
/// since once publication has completed the snapshot is observable and the tick is closed, so that region must
/// be throw-free by construction rather than recoverable." This fixture is the enforcement point the clause
/// names for that region.
/// </para>
/// <para>
/// <b>The property this defends, and why a remark could not defend it.</b> Recovery from a mid-commit
/// failure is invalidating the publisher's open tick, and that is available exactly until the snapshot
/// becomes observable and the tick closes. Everything after that instant is unrecoverable, so the statements
/// there must not be able to throw. <c>docs/technical/20-simulation-core.md</c> § Entity identity now extends
/// its enforcement-point convention past ordered collections to any normative rule in that document, so a rule
/// naming no enforcement point is an omission rather than an exemption; that is what this fixture answers to
/// rather than to the ordered-collection wording alone. "Nothing after the publication can throw" is a property
/// that decays silently the first time someone adds a statement, because no existing test notices and the
/// remark still reads as true. This is that enforcement point.
/// </para>
/// <para>
/// <b>The region cannot simply be made empty, which is why it needs a gate at all.</b> In
/// <c>SnapshotPublisher.Publish</c> the two batch views, the policy name, and both run-session fences were
/// hoisted above the page flip, leaving only work that assigns fields. In
/// <c>CommandAdmissionGate.CommitApplied</c> seven member calls genuinely have to follow the publication: the
/// buffer release ends the lease the publication opened, the state version and the applied counter must not
/// advance for a publication that failed, and the result and its history entry are built from the
/// publication's own version and event count. So the region is small, fixed, and pinned rather than absent.
/// </para>
/// <para>
/// <b>Compiled bodies, not source text.</b> A source scan would have to model comments, string literals, and
/// formatting; an IL walk sees the calls the compiler actually emitted. It follows the precedent
/// <see cref="SimulationAssemblyDeterminismTests"/> sets for inspecting the built artifact rather than the
/// text that produced it. <see cref="TheScanIsHonestAboutWhatItCannotSee"/> records what the walk does and
/// does not reach.
/// </para>
/// </remarks>
[TestFixture]
internal sealed class PostPublicationRegionTests
{
    /// <summary>The assembly whose members the region is allowed to name.</summary>
    private const string SimulationAssemblyName = "MechaMiner.Simulation";

    /// <summary>
    /// Every IL opcode by its encoded value, so the walk knows each instruction's operand size.
    /// </summary>
    private static readonly Dictionary<short, OpCode> OpCodesByValue = BuildOpCodeTable();

    /// <summary>
    /// Verification: supports <c>VER-SIM-004-007</c>.
    ///
    /// After <c>SnapshotDoubleBuffer.Publish</c> flips the page, <c>SnapshotPublisher.Publish</c> constructs
    /// nothing, throws nothing, and calls only the two simulation members that assign the tick's result.
    /// </summary>
    [Test]
    public void NothingAfterThePageFlipInAPublicationCanThrow()
    {
        MethodInfo publish = SimulationMethod(
            typeof(SnapshotPublisher),
            "Publish",
            BindingFlags.Public | BindingFlags.Instance);

        RegionScan scan = ScanRegionAfter(publish, nameof(SnapshotDoubleBuffer), "Publish");

        AssertRegionIsThrowFree(
            "SnapshotPublisher.Publish after the page flip",
            scan,
            expectedSimulationMembers:
            [
                "DomainEventBuffer.RecordAllConsumed",
                "TickPublication.Published",
            ]);
    }

    /// <summary>
    /// Verification: supports <c>VER-SIM-004-013</c>.
    ///
    /// After <c>SnapshotPublisher.Publish</c> returns, <c>CommandAdmissionGate.CommitApplied</c> constructs
    /// nothing, throws nothing, and calls only the seven simulation members that record the applied commit.
    /// </summary>
    /// <remarks>
    /// The seven are the whole of the unrecoverable region, and each is throw-free for a reason the commit
    /// establishes rather than assumes: the release is over buffers the commit opened and whose records the
    /// publication has just marked consumed; the two <c>TickPublication</c> getters and
    /// <c>SnapshotVersion.ToString</c> read a value the publication just returned; the request's action ID
    /// and client command sequence are field reads on a request <c>Apply</c> has already required to be
    /// present; and
    /// <c>PausedTransactionResult.Accepted</c>'s preconditions are that presence, the event's completeness
    /// which <c>DomainEventBuffer.Append</c> established, a positive version computed under <c>checked</c>
    /// above the publication, and a detail built from literals.
    /// </remarks>
    [Test]
    public void NothingAfterThePublicationInATransactionCommitCanThrow()
    {
        MethodInfo commitApplied = SimulationMethod(
            typeof(CommandAdmissionGate),
            "CommitApplied",
            BindingFlags.NonPublic | BindingFlags.Instance);

        RegionScan scan = ScanRegionAfter(commitApplied, nameof(SnapshotPublisher), "Publish");

        AssertRegionIsThrowFree(
            "CommandAdmissionGate.CommitApplied after the publication",
            scan,
            expectedSimulationMembers:
            [
                "PausedTransactionRequest.get_ActionId",
                "PausedTransactionRequest.get_ClientCommandSequence",
                "PausedTransactionResult.Accepted",
                "SnapshotPublisher.ReleaseTick",
                "SnapshotVersion.ToString",
                "TickPublication.get_DomainEventCount",
                "TickPublication.get_Version",
            ]);
    }

    /// <summary>
    /// Proves the scan can fail, by running it over methods whose region after the anchor deliberately
    /// throws, constructs, and calls an extra simulation member.
    /// </summary>
    /// <remarks>
    /// doc 91 § Acceptance evidence wants evidence a gate can fail. Without this, a walk that silently
    /// stopped at the anchor - or that resolved no tokens at all - would report an empty, throw-free region
    /// for every method and the two tests above would pass unconditionally.
    /// </remarks>
    [Test]
    public void TheScanDetectsAThrowAConstructionAndAnExtraCallAfterTheAnchor()
    {
        RegionScan throwing = ScanRegionAfter(
            SimulationTestMethod(nameof(AnchorThenThrow)),
            nameof(PostPublicationRegionTests),
            nameof(Anchor));
        RegionScan calling = ScanRegionAfter(
            SimulationTestMethod(nameof(AnchorThenCallASimulationMember)),
            nameof(PostPublicationRegionTests),
            nameof(Anchor));

        Expect.Multiple(() =>
        {
            Assert.That(
                throwing.Throws,
                Is.EqualTo(1),
                "the walk must see the throw after the anchor, or the two gates above are vacuous");
            Assert.That(
                throwing.ObjectConstructions,
                Is.EqualTo(1),
                "and the exception's construction");
            Assert.That(
                calling.SimulationMembersCalled,
                Does.Contain("SnapshotPublisher.InvalidateTick"),
                "and a simulation call the expected list does not name");
            Assert.That(
                calling.Throws,
                Is.EqualTo(0),
                "while a region that only calls must not be reported as throwing, or the two findings are "
                    + "not independent");
        });
    }

    /// <summary>
    /// Records what the IL walk reaches and what it does not, so the gate's limits are written down rather
    /// than discovered.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The walk reaches every <c>call</c>, <c>callvirt</c>, and <c>newobj</c> in the region, resolves each to
    /// its declaring type, and reads a <c>constrained.</c> prefix so that a virtual call on a simulation
    /// value type is reported under that type rather than as <c>Object.ToString</c>. It does not reach: a
    /// call made through a delegate or a function pointer, where the target is a value rather than a token;
    /// a member reached by reflection; or an exception raised by the runtime itself rather than by a
    /// statement, such as a stack overflow or an out-of-memory failure, neither of which any arrangement of
    /// statements can exclude. The last of those is the honest boundary of the whole property: the region is
    /// free of throws a statement can cause, not of every conceivable failure.
    /// </para>
    /// <para>
    /// A fifth limit is not about what the walk reaches but about what a green run proves. The committed
    /// allowed-call lists in the two gates above, the two members in
    /// <see cref="NothingAfterThePageFlipInAPublicationCanThrow"/> and the seven in
    /// <see cref="NothingAfterThePublicationInATransactionCommitCanThrow"/>, are a two-place edit tax rather
    /// than evidence. Each list is a transcription of what its region already calls, so someone who adds a
    /// call to the region and also adds it to the list gets a green run, and nothing in either gate
    /// establishes that the added member cannot throw. What the tax buys is that an <em>accidental</em>
    /// addition is loud, which <see cref="TheScanDetectsAThrowAConstructionAndAnExtraCallAfterTheAnchor"/>
    /// proves and which is worth having on its own terms; why each listed member is throw-free is argued in
    /// that gate's remarks and is not demonstrated by this walk, so
    /// <c>docs/technical/91-verification-strategy.md</c> § Acceptance evidence is satisfied about the gate's
    /// ability to fail and not about the property the list appears to certify. This is the house position on
    /// a committed inventory rather than a criticism of the design, and the same ruling was made about the
    /// verification registry's census ceiling and the architecture gate's forbidden-edge count: a pinned
    /// inventory makes drift loud, and it is never a proof of the property the inventory is about.
    /// </para>
    /// </remarks>
    [Test]
    public void TheScanIsHonestAboutWhatItCannotSee()
    {
        MethodInfo commitApplied = SimulationMethod(
            typeof(CommandAdmissionGate),
            "CommitApplied",
            BindingFlags.NonPublic | BindingFlags.Instance);

        // Anchored at the start of the commit rather than at its publication, so this region strictly
        // contains the one the gate above pins - including the staging callback, which is the blind spot.
        RegionScan wholeCommit = ScanRegionAfter(commitApplied, nameof(SnapshotPublisher), "BeginTick");
        RegionScan afterPublication = ScanRegionAfter(commitApplied, nameof(SnapshotPublisher), "Publish");

        Expect.Multiple(() =>
        {
            // The delegate blind spot, stated against the very method that has one: the staging callback is
            // invoked through Action<T>.Invoke, which carries no token for the code it reaches. It sits
            // before the publication, which is why the blind spot costs nothing here - and why the region
            // after the publication must not acquire a delegate call.
            Assert.That(
                wholeCommit.SimulationMembersCalled,
                Does.Not.Contain("Action`1.Invoke"),
                "a delegate invocation resolves to the delegate type, not to the simulation code it "
                    + "reaches, so the walk cannot see through one");
            Assert.That(
                afterPublication.SimulationMembersCalled,
                Is.Not.Empty,
                "the anchored region must be non-empty, or an anchor that failed to match would report a "
                    + "clean region for any method at all");
            Assert.That(
                wholeCommit.SimulationMembersCalled.Count,
                Is.GreaterThan(afterPublication.SimulationMembersCalled.Count),
                "the commit from its BeginTick onwards must call more than the region after its "
                    + "publication, or the anchor is matching the wrong instruction and the region pinned "
                    + "is not the one the remark describes");
            Assert.That(
                wholeCommit.ObjectConstructions,
                Is.EqualTo(0),
                "the commit happens to construct nothing anywhere, which is worth recording: the gate "
                    + "above would still hold if it did, since only the region after the publication is "
                    + "constrained");
        });
    }

    /// <summary>The anchor the self-control's regions are measured from.</summary>
    private static void Anchor()
    {
    }

    /// <summary>A region that throws after the anchor, for the self-control.</summary>
    private static void AnchorThenThrow()
    {
        Anchor();
        throw new InvalidOperationException("a deliberate failure, so the scan has something to find");
    }

    /// <summary>A region that calls an unexpected simulation member after the anchor, for the self-control.</summary>
    /// <remarks>
    /// Never invoked: the publisher argument is only there to give the call site a receiver, and the method
    /// exists so that the compiler emits the call the scan must notice.
    /// </remarks>
    private static void AnchorThenCallASimulationMember(SnapshotPublisher publisher)
    {
        Anchor();
        publisher.InvalidateTick("a deliberate call, so the scan has something to find");
    }

    private static MethodInfo SimulationTestMethod(string name)
    {
        MethodInfo? method = typeof(PostPublicationRegionTests).GetMethod(
            name,
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null, "the self-control method " + name + " must exist");
        return method!;
    }

    private static MethodInfo SimulationMethod(Type declaringType, string name, BindingFlags flags)
    {
        MethodInfo? method = declaringType.GetMethod(name, flags);
        Assert.That(
            method,
            Is.Not.Null,
            declaringType.Name + "." + name + " must exist, or this gate is pinning nothing");
        Assert.That(
            method!.DeclaringType!.Assembly.GetName().Name,
            Is.EqualTo(SimulationAssemblyName),
            "the method must come from the simulation assembly under test");
        return method;
    }

    /// <summary>
    /// Asserts a region constructs nothing, throws nothing, and calls exactly
    /// <paramref name="expectedSimulationMembers"/>.
    /// </summary>
    /// <param name="subject">What the region is, for the failure message.</param>
    /// <param name="scan">The region's findings.</param>
    /// <param name="expectedSimulationMembers">
    /// The simulation members the region may call, sorted ordinally.
    /// </param>
    private static void AssertRegionIsThrowFree(
        string subject,
        RegionScan scan,
        string[] expectedSimulationMembers)
    {
        Expect.Multiple(() =>
        {
            Assert.That(
                scan.SimulationMembersCalled,
                Is.EqualTo(expectedSimulationMembers),
                subject + ": these are the simulation members the region may call. A member added here is "
                    + "an unrecoverable statement: the snapshot is already observable and the tick already "
                    + "closed, so InvalidateTick cannot retract it and TR-RUN-007's \"does not publish "
                    + "partial state\" would no longer hold. Move the statement above the publication, or "
                    + "establish here why it cannot throw and add it to this list");
            Assert.That(
                scan.ObjectConstructions,
                Is.EqualTo(0),
                subject + ": the region must construct nothing. A construction is either an exception being "
                    + "raised or a constructor that can validate, and both are throws in a region that "
                    + "cannot be unwound");
            Assert.That(
                scan.Throws,
                Is.EqualTo(0),
                subject + ": the region must contain no throw or rethrow instruction");
        });
    }

    /// <summary>
    /// Walks a method's compiled body and reports what happens after the last call to
    /// <paramref name="anchorTypeName"/>.<paramref name="anchorMemberName"/>.
    /// </summary>
    /// <param name="method">The method to read.</param>
    /// <param name="anchorTypeName">The declaring type of the anchoring call.</param>
    /// <param name="anchorMemberName">The anchoring call's member name.</param>
    /// <remarks>
    /// The <em>last</em> matching call, so a method that reaches its publication once cannot hide statements
    /// behind an earlier call of the same name.
    /// </remarks>
    private static RegionScan ScanRegionAfter(
        MethodInfo method,
        string anchorTypeName,
        string anchorMemberName)
    {
        List<Instruction> instructions = Decode(method);
        string anchor = anchorTypeName + "." + anchorMemberName;

        int anchorIndex = -1;
        for (int index = 0; index < instructions.Count; index++)
        {
            if (string.Equals(instructions[index].CalledMember, anchor, StringComparison.Ordinal))
            {
                anchorIndex = index;
            }
        }

        Assert.That(
            anchorIndex,
            Is.GreaterThanOrEqualTo(0),
            "the anchoring call to " + anchor + " must appear in " + method.Name
                + ", or the region measured would be the whole method");

        SortedSet<string> called = new(StringComparer.Ordinal);
        int constructions = 0;
        int throws = 0;
        for (int index = anchorIndex + 1; index < instructions.Count; index++)
        {
            Instruction instruction = instructions[index];
            if (instruction.IsConstruction)
            {
                constructions++;
            }

            if (instruction.IsThrow)
            {
                throws++;
            }

            if (instruction.CalledMember.Length != 0 && instruction.IsSimulationMember)
            {
                called.Add(instruction.CalledMember);
            }
        }

        List<string> sorted = new(called);
        return new RegionScan(sorted, constructions, throws);
    }

    /// <summary>
    /// Decodes a method's IL into one record per instruction, resolving call, construction, and
    /// <c>constrained.</c> tokens.
    /// </summary>
    /// <param name="method">The method to decode.</param>
    private static List<Instruction> Decode(MethodInfo method)
    {
        MethodBody? body = method.GetMethodBody();
        Assert.That(body, Is.Not.Null, method.Name + " must have a compiled body to read");
        byte[]? il = body!.GetILAsByteArray();
        Assert.That(il, Is.Not.Null, method.Name + " must expose its IL");

        List<Instruction> instructions = new();
        Module module = method.Module;
        Type? constrainedType = null;
        int position = 0;
        while (position < il!.Length)
        {
            short value = il[position];
            position++;
            if (value == 0xFE)
            {
                value = unchecked((short)(0xFE00 | il[position]));
                position++;
            }

            OpCode opCode = OpCodesByValue[value];
            int operandStart = position;
            position += OperandSize(opCode, il, operandStart);

            if (opCode == OpCodes.Constrained)
            {
                constrainedType = ResolveTypeOrNull(module, BitConverter.ToInt32(il, operandStart));
                continue;
            }

            bool isCall = opCode == OpCodes.Call || opCode == OpCodes.Callvirt;
            bool isConstruction = opCode == OpCodes.Newobj;
            string calledMember = string.Empty;
            bool isSimulationMember = false;
            if (isCall || isConstruction)
            {
                MethodBase? target = ResolveMethodOrNull(module, BitConverter.ToInt32(il, operandStart));
                Type? owner = constrainedType ?? target?.DeclaringType;
                if (target is not null && owner is not null)
                {
                    calledMember = owner.Name + "." + target.Name;
                    isSimulationMember = string.Equals(
                        owner.Assembly.GetName().Name,
                        SimulationAssemblyName,
                        StringComparison.Ordinal);
                }
            }

            constrainedType = null;
            instructions.Add(new Instruction(
                calledMember,
                isSimulationMember,
                isConstruction,
                opCode == OpCodes.Throw || opCode == OpCodes.Rethrow));
        }

        return instructions;
    }

    private static MethodBase? ResolveMethodOrNull(Module module, int token)
    {
        try
        {
            return module.ResolveMethod(token);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static Type? ResolveTypeOrNull(Module module, int token)
    {
        try
        {
            return module.ResolveType(token);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>How many operand bytes an instruction carries.</summary>
    /// <param name="opCode">The decoded opcode.</param>
    /// <param name="il">The method body.</param>
    /// <param name="operandStart">Where the operand begins.</param>
    private static int OperandSize(OpCode opCode, byte[] il, int operandStart)
    {
        switch (opCode.OperandType)
        {
            case OperandType.InlineNone:
                return 0;
            case OperandType.ShortInlineBrTarget:
            case OperandType.ShortInlineI:
            case OperandType.ShortInlineVar:
                return 1;
            case OperandType.InlineVar:
                return 2;
            case OperandType.InlineBrTarget:
            case OperandType.InlineField:
            case OperandType.InlineI:
            case OperandType.InlineMethod:
            case OperandType.InlineSig:
            case OperandType.InlineString:
            case OperandType.InlineTok:
            case OperandType.InlineType:
            case OperandType.ShortInlineR:
                return 4;
            case OperandType.InlineI8:
            case OperandType.InlineR:
                return 8;
            case OperandType.InlineSwitch:
                return 4 + (4 * BitConverter.ToInt32(il, operandStart));
            default:
                throw new InvalidOperationException(
                    "unhandled operand type "
                        + opCode.OperandType.ToString()
                        + " for opcode "
                        + opCode.Name);
        }
    }

    private static Dictionary<short, OpCode> BuildOpCodeTable()
    {
        Dictionary<short, OpCode> table = new();
        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                table[opCode.Value] = opCode;
            }
        }

        Assert.That(
            table,
            Is.Not.Empty,
            "the opcode table must be populated, or every method decodes as an empty region: "
                + table.Count.ToString(CultureInfo.InvariantCulture));
        return table;
    }

    /// <summary>One decoded instruction, reduced to the three facts this gate reads.</summary>
    /// <param name="CalledMember">
    /// The called or constructed member as <c>Type.Member</c>, or empty when the instruction is neither.
    /// </param>
    /// <param name="IsSimulationMember">Whether that member is declared in the simulation assembly.</param>
    /// <param name="IsConstruction">Whether the instruction constructs an object.</param>
    /// <param name="IsThrow">Whether the instruction throws or rethrows.</param>
    private readonly record struct Instruction(
        string CalledMember,
        bool IsSimulationMember,
        bool IsConstruction,
        bool IsThrow);

    /// <summary>What a region after an anchoring call does.</summary>
    /// <param name="SimulationMembersCalled">
    /// The distinct simulation members it calls, sorted ordinally.
    /// </param>
    /// <param name="ObjectConstructions">How many objects it constructs.</param>
    /// <param name="Throws">How many throw or rethrow instructions it contains.</param>
    private readonly record struct RegionScan(
        IReadOnlyList<string> SimulationMembersCalled,
        int ObjectConstructions,
        int Throws);
}
