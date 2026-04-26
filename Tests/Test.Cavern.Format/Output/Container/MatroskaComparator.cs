using Cavern.Format.Container.Matroska;

namespace Test.Cavern.Format.Output.Container;

/// <summary>
/// Tag by tag, compares if two Matroska files are identical.
/// </summary>
static class MatroskaComparator {
    public static void Compare(string referencePath, string resultPath) {
        using FileStream referenceStream = File.OpenRead(referencePath);
        using FileStream resultStream = File.OpenRead(resultPath);
        MatroskaTree[] reference = ParseRootElements(referenceStream);
        MatroskaTree[] result = ParseRootElements(resultStream);
        Compare(reference, referenceStream, result, resultStream, "root");
    }

    /// <summary>
    /// Recursively compares two Matroska node children sets, throws if they differ in any way.
    /// </summary>
    static void Compare(MatroskaTree[] referenceTrees, FileStream referenceStream, MatroskaTree[] resultTrees, FileStream resultStream, string tagPath) {
        if (referenceTrees.Length != resultTrees.Length) {
            throw new Exception($"Child count mismatch for {tagPath}: {referenceTrees.Length} vs {resultTrees.Length}.");
        }
        for (int i = 0; i < referenceTrees.Length; i++) {
            Compare(referenceTrees[i], referenceStream, resultTrees[i], resultStream, tagPath);
        }
    }

    /// <summary>
    /// Recursively compares two Matroska nodes, throws if they differ in any way.
    /// </summary>
    static void Compare(MatroskaTree referenceTree, FileStream referenceStream, MatroskaTree resultTree, FileStream resultStream, string tagPath) {
        if (referenceTree.Tag != resultTree.Tag) {
            throw new Exception($"Tag mismatch under {tagPath}: 0x{referenceTree.Tag:X} vs 0x{resultTree.Tag:X}.");
        }
        tagPath = $"{tagPath}/{referenceTree.Tag:X}";
        if (referenceTree.Length != resultTree.Length) {
            throw new Exception($"Length mismatch for {tagPath}: {referenceTree.Length} vs {resultTree.Length}.");
        }

        if (!referenceTree.HasChildren()) {
            byte[] referenceBytes = referenceTree.GetBytes(referenceStream);
            byte[] resultBytes = resultTree.GetBytes(resultStream);
            if (!referenceBytes.SequenceEqual(resultBytes)) {
                throw new Exception($"Data mismatch for {tagPath}.");
            }
            return;
        }

        MatroskaTree[] referenceChildren = referenceTree.GetChildren(referenceStream);
        MatroskaTree[] resultChildren = resultTree.GetChildren(resultStream);
        Compare(referenceChildren, referenceStream, resultChildren, resultStream, tagPath);
    }

    /// <summary>
    /// Read all tree roots from a Matroska file.
    /// </summary>
    static MatroskaTree[] ParseRootElements(FileStream stream) {
        List<MatroskaTree> elements = [];
        while (stream.Position < stream.Length) {
            elements.Add(new MatroskaTree(stream));
        }
        return [.. elements];
    }
}
