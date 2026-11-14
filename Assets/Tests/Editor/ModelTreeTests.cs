using NUnit.Framework;

public class ModelTreeTests
{
    [Test]
    public void LinkingAndDeletingASubtreeNeedsNoRuntimePlayer()
    {
        var root = new VChip(new[] { VChip.typeStr }, new[] { VChip.coreStr }, 0, null);
        var child = new VChip(VChip.chipStr, LocalDirection.North, root);
        var grandchild = new VChip(VChip.chipStr, LocalDirection.North, child);
        var sibling = new VChip(VChip.chipStr, LocalDirection.South, root);
        var model = new VModel { chips = new[] { root, child, grandchild, sibling } };
        CollectionAssert.AreEquivalent(new[] { child, sibling }, root.Children);
        CollectionAssert.AreEqual(new[] { grandchild }, child.Children);
        Assert.That(grandchild.MyModel, Is.SameAs(model));
        model.DeleteChip(child.id);
        CollectionAssert.AreEquivalent(new[] { root, sibling }, model.chips);
        CollectionAssert.AreEqual(new[] { sibling }, root.Children);
    }
}
