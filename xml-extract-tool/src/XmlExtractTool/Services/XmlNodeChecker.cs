using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using XmlExtractTool.Models;
using XmlExtractTool.Utils;

namespace XmlExtractTool.Services
{
    /// <summary>
    /// Full node checker: folder scan, file formats, nodeType/parent/rotate/translate and LoopMode rules.
    /// </summary>
    public class XmlNodeChecker
    {
        private const double RotateTolerance = 1e-5;
        private const double TranslateTolerance = 1e-5;

        private readonly CheckerSettings _settings;

        public XmlNodeChecker(CheckerSettings settings)
        {
            _settings = settings ?? new CheckerSettings();
        }

        /// <summary>
        /// Check a single file; same rules as folder mode. Returns collected errors for that file.
        /// </summary>
        public List<CheckResultItem> CheckFile(string filePath)
        {
            var results = new List<CheckResultItem>();
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return results;

            var extensions = _settings.GetFileExtensionList();
            var ext = Path.GetExtension(filePath).TrimStart('.');
            if (!extensions.Any(e => e.TrimStart('.').Equals(ext, StringComparison.OrdinalIgnoreCase)))
                return results;

            var fileName = Path.GetFileName(filePath);
            try
            {
                var doc = XmlHelper.LoadXml(filePath);
                if (!XmlHelper.IsValidXml(doc))
                    return results;

                // LoopMode: only report when value is explicitly wrong (1 vs 2). Never report when "0", null or empty.
                if (filePath.EndsWith(".upe", StringComparison.OrdinalIgnoreCase))
                {
                    var timeline = doc.Root?.Element("Timeline");
                    var loopMode = timeline?.Attribute("LoopMode")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(loopMode))
                    {
                        var keywords2 = _settings.GetKeywordsLoopMode2();
                        var keywords1 = _settings.GetKeywordsLoopMode1();
                        if (CheckerSettings.MatchesAnyPattern(fileName, keywords2) && loopMode == "1")
                            results.Add(new CheckResultItem { FileName = fileName, NodeName = "LoopMode", Parent = "应为 2" });
                            // 2065*_hand is allowed to have LoopMode=2 (correct sample uses 2)
                            if (CheckerSettings.MatchesAnyPattern(fileName, keywords1) && loopMode == "2" &&
                                !CheckerSettings.FileNameMatchesPattern(fileName, "2065*_hand.upe"))
                                results.Add(new CheckResultItem { FileName = fileName, NodeName = "LoopMode", Parent = "应为 1" });
                    }
                }

                var nodesContainer = doc.Root?.Element("Tree")?.Element("Nodes") ?? doc.Root?.Element("Nodes");
                var nodeElements = nodesContainer?.Elements("Node").ToList() ?? doc.Root?.Descendants("Node").ToList();
                if (nodeElements == null || nodeElements.Count == 0)
                    return results;

                string? firstNodeName = null;
                var nodeInfos = new List<NodeInfo>();
                foreach (var el in nodeElements)
                {
                    var ni = ParseNode(el);
                    if (ni == null) continue;
                    if (firstNodeName == null) firstNodeName = ni.Name;
                    nodeInfos.Add(ni);
                }

                bool is2065Hand = CheckerSettings.FileNameMatchesPattern(fileName, "2065*hand.upe") ||
                                 CheckerSettings.FileNameMatchesPattern(fileName, "2065*_hand.upe");
                bool is2065Love3Didle = CheckerSettings.FileNameMatchesPattern(fileName, "2065*love_3didle.upe");

                var effectSubtreeNames = GetEffectSubtreeNames(nodeInfos);
                foreach (var ni in nodeInfos)
                {
                    if (IsFirstNodeWithNoParent(ni, firstNodeName))
                        continue;
                    if (IsError(ni, firstNodeName!, is2065Hand, is2065Love3Didle, effectSubtreeNames))
                        results.Add(new CheckResultItem { FileName = fileName, NodeName = ni.Name, Parent = ni.Parent });
                }
            }
            catch { }

            return results;
        }

        /// <summary>
        /// Scan folder and check all matching files; return collected errors (FileName, NodeName, Parent).
        /// </summary>
        public List<CheckResultItem> CheckFolder(string folderPath)
        {
            var results = new List<CheckResultItem>();
            if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
                return results;

            var extensions = _settings.GetFileExtensionList();
            var files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories)
                .Where(f => extensions.Any(ext => f.EndsWith(ext.TrimStart('.'), StringComparison.OrdinalIgnoreCase)))
                .ToList();

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                try
                {
                    var doc = XmlHelper.LoadXml(filePath);
                    if (!XmlHelper.IsValidXml(doc))
                        continue;

                    // LoopMode: only report when value is explicitly wrong (1 vs 2). Never report when "0", null or empty.
                    if (filePath.EndsWith(".upe", StringComparison.OrdinalIgnoreCase))
                    {
                        var timeline = doc.Root?.Element("Timeline");
                        var loopMode = timeline?.Attribute("LoopMode")?.Value?.Trim();
                        if (!string.IsNullOrEmpty(loopMode))
                        {
                            var keywords2 = _settings.GetKeywordsLoopMode2();
                            var keywords1 = _settings.GetKeywordsLoopMode1();
                            if (CheckerSettings.MatchesAnyPattern(fileName, keywords2) && loopMode == "1")
                                results.Add(new CheckResultItem { FileName = fileName, NodeName = "LoopMode", Parent = "应为 2" });
                            // 2065*_hand is allowed to have LoopMode=2 (correct sample uses 2)
                            if (CheckerSettings.MatchesAnyPattern(fileName, keywords1) && loopMode == "2" &&
                                !CheckerSettings.FileNameMatchesPattern(fileName, "2065*_hand.upe"))
                                results.Add(new CheckResultItem { FileName = fileName, NodeName = "LoopMode", Parent = "应为 1" });
                        }
                    }

                    var nodesContainer = doc.Root?.Element("Tree")?.Element("Nodes") ?? doc.Root?.Element("Nodes");
                    var nodeElements = nodesContainer?.Elements("Node").ToList() ?? doc.Root?.Descendants("Node").ToList();
                    if (nodeElements == null || nodeElements.Count == 0)
                        continue;

                    string? firstNodeName = null;
                    var nodeInfos = new List<NodeInfo>();
                    foreach (var el in nodeElements)
                    {
                        var ni = ParseNode(el);
                        if (ni == null) continue;
                        if (firstNodeName == null) firstNodeName = ni.Name;
                        nodeInfos.Add(ni);
                    }

                    bool is2065Hand = CheckerSettings.FileNameMatchesPattern(fileName, "2065*hand.upe") ||
                                      CheckerSettings.FileNameMatchesPattern(fileName, "2065*_hand.upe");
                    bool is2065Love3Didle = CheckerSettings.FileNameMatchesPattern(fileName, "2065*love_3didle.upe");

                    var effectSubtreeNames = GetEffectSubtreeNames(nodeInfos);
                    foreach (var ni in nodeInfos)
                    {
                        if (IsFirstNodeWithNoParent(ni, firstNodeName))
                            continue;

                        if (IsError(ni, firstNodeName!, is2065Hand, is2065Love3Didle, effectSubtreeNames))
                            results.Add(new CheckResultItem { FileName = fileName, NodeName = ni.Name, Parent = ni.Parent });
                    }
                }
                catch
                {
                    // Skip file on parse error
                }
            }

            return results;
        }

        /// <summary>
        /// First node with no parent or parent=own name: do not show in results (per user spec).
        /// </summary>
        private static bool IsFirstNodeWithNoParent(NodeInfo ni, string? firstNodeName)
        {
            if (firstNodeName == null) return false;
            if (ni.Name != firstNodeName) return false;
            var p = ni.Parent ?? string.Empty;
            return string.IsNullOrWhiteSpace(p) || p.Trim() == ni.Name;
        }

        /// <summary>
        /// Get set of node names that belong to effect subtree (under FX* root). Used to skip transform check for effect nodes.
        /// </summary>
        private static HashSet<string> GetEffectSubtreeNames(List<NodeInfo> nodeInfos)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var parentStartsWithFx = (string p) => p.Length >= 2 && p.StartsWith("FX", StringComparison.OrdinalIgnoreCase) &&
                (p.Length == 2 || char.IsDigit(p[2]) || p[2] == ' ');
            bool added;
            do
            {
                added = false;
                foreach (var ni in nodeInfos)
                {
                    if (result.Contains(ni.Name)) continue;
                    bool underFx = parentStartsWithFx(ni.Parent ?? "") || (result.Count > 0 && result.Contains(ni.Parent ?? ""));
                    if (underFx && result.Add(ni.Name))
                        added = true;
                }
            } while (added);
            return result;
        }

        private bool IsError(NodeInfo ni, string firstNodeName, bool is2065Hand, bool is2065Love3Didle, HashSet<string>? effectSubtreeNames)
        {
            // Node named x_link is the link node; do not flag
            if (string.Equals(ni.Name, "x_link", StringComparison.OrdinalIgnoreCase))
                return false;

            // BoneNode (skeleton bones) are structural; do not flag
            if (string.Equals(ni.NodeType, "BoneNode", StringComparison.OrdinalIgnoreCase))
                return false;

            // Nodes in effect subtree (under FX root) are allowed any transform
            if (effectSubtreeNames != null && effectSubtreeNames.Contains(ni.Name))
                return false;

            // Empty mount point (空挂点) under Hand is allowed; also match "挂点", or "1a"+"(1)" when encoding breaks 空挂点
            var parentHasHand = ni.Parent?.Contains("Hand", StringComparison.Ordinal) ?? false;
            var nameLooksLikeMount = ni.Name.Contains("空挂点", StringComparison.Ordinal) ||
                ni.Name.Contains("挂点", StringComparison.Ordinal) ||
                (ni.Name.Contains("(1)", StringComparison.Ordinal) && ni.Name.Contains("1a", StringComparison.Ordinal));
            if (parentHasHand && nameLooksLikeMount)
                return false;

            // Children of 空挂点 (parent name contains 空挂点) are allowed
            if (ni.Parent != null && (ni.Parent.Contains("空挂点", StringComparison.Ordinal) || ni.Parent.Contains("挂点", StringComparison.Ordinal)))
                return false;

            // ParticleNode (particle effects) are allowed any transform
            if (string.Equals(ni.NodeType, "ParticleNode", StringComparison.OrdinalIgnoreCase))
                return false;

            var q = ni.Quaternion;
            var t = ni.Translate;
            bool hasRotate = q.HasValue;
            bool rotZero = q?.IsZeroRotation(RotateTolerance) ?? true;
            bool transZero = !t.HasValue || t.Value.IsZero(TranslateTolerance);

            // EModelNode under root Bip01 (not Hand/Clavicle/Head): allow zero rotate+translate, or ~90° rotation (any axis) with translate ignored (display offset)
            if (string.Equals(ni.NodeType, "EModelNode", StringComparison.OrdinalIgnoreCase))
            {
                var p = ni.Parent ?? "";
                bool underRootBip01 = p.Length > 0 && p.Contains("Bip01", StringComparison.Ordinal) &&
                    !p.Contains("Hand", StringComparison.Ordinal) &&
                    !p.Contains("Clavicle", StringComparison.Ordinal) &&
                    !p.Contains("Head", StringComparison.Ordinal);
                if (underRootBip01)
                {
                    // Allow: zero rotate + zero translate, or ~90° rotation (any axis) with translate ignored (display offset)
                    if (rotZero && transZero)
                        return false;
                    if (hasRotate && q!.Value.Is90DegreeRotation(1.0))
                        return false;
                    return true;
                }
                return false; // EModelNode under Hand/FX/other is allowed any transform
            }

            // ESklModelNode under Bip01 L Hand / R Hand: same rule as x_link (X ~90°, zero translate); "shouji" node allows small display offset
            if (string.Equals(ni.NodeType, "ESklModelNode", StringComparison.OrdinalIgnoreCase))
            {
                var p = ni.Parent ?? "";
                if ((p.Contains("Bip01", StringComparison.Ordinal) && p.Contains("Hand", StringComparison.Ordinal)) &&
                    (p.Contains("L Hand", StringComparison.Ordinal) || p.Contains("R Hand", StringComparison.Ordinal)))
                {
                    if (!hasRotate || !q!.Value.IsXAxis90Degree(RotateTolerance))
                        return true;
                    if (ni.Name.Contains("shouji", StringComparison.OrdinalIgnoreCase))
                        return false; // allow display offset for shouji under Hand
                    if (!transZero)
                        return true;
                    return false;
                }
                // ESklModelNode under Bip01 L Clavicle / Bip01 Head: X ~90°, zero translate (same as Hand)
                if (p.Contains("Bip01", StringComparison.Ordinal) &&
                    (p.Contains("Clavicle", StringComparison.Ordinal) || p.Contains("Head", StringComparison.Ordinal)))
                {
                    if (!hasRotate || !q!.Value.IsXAxis90Degree(RotateTolerance))
                        return true;
                    if (!transZero)
                        return true;
                    return false;
                }
            }

            // parent == x_link: X ~90°, Y/Z 0°, translate ignore
            if (string.Equals(ni.Parent, "x_link", StringComparison.OrdinalIgnoreCase))
            {
                if (!hasRotate || !q!.Value.IsXAxis90Degree(RotateTolerance))
                    return true;
                return false;
            }

            // parent == own name (self)
            if (string.Equals(ni.Parent, ni.Name, StringComparison.Ordinal))
            {
                if (is2065Love3Didle)
                {
                    if (!hasRotate || !q!.Value.IsYAxis90Degree(RotateTolerance))
                        return true;
                    return false;
                }
                if (!rotZero || !transZero)
                    return true;
                return false;
            }

            // parent == first Node name (root)
            if (string.Equals(ni.Parent, firstNodeName, StringComparison.Ordinal))
            {
                if (string.Equals(ni.NodeType, "ESklModelNode", StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasRotate || !q!.Value.IsXAxis90Degree(RotateTolerance))
                        return true;
                    if (!is2065Hand && !transZero)
                        return true;
                    return false;
                }
                if (!rotZero || !transZero)
                    return true;
                return false;
            }

            // nodeType not ESklModelNode and parent not x_link: all rotate and translate 0
            if (!string.Equals(ni.NodeType, "ESklModelNode", StringComparison.OrdinalIgnoreCase))
            {
                if (!rotZero || !transZero)
                    return true;
                return false;
            }

            // ESklModelNode but parent not first and not x_link and not self and not Hand -> require all zero
            if (!rotZero || !transZero)
                return true;
            return false;
        }

        private static NodeInfo? ParseNode(XElement node)
        {
            var nameAttribute = node.Attribute("name");
            if (nameAttribute == null || string.IsNullOrWhiteSpace(nameAttribute.Value))
                return null;

            var nodeInfo = new NodeInfo
            {
                Name = nameAttribute.Value,
                NodeType = node.Attribute("nodeType")?.Value ?? string.Empty,
                Parent = node.Attribute("parent")?.Value ?? string.Empty
            };

            var keyFrame = node.Element("Animation")?.Elements("KeyFrame").FirstOrDefault();
            if (keyFrame == null)
                return nodeInfo;

            var rotateAttr = keyFrame.Attribute("rotate");
            if (rotateAttr != null && Quaternion.TryParse(rotateAttr.Value, out Quaternion q))
                nodeInfo.Quaternion = q;
            var translateAttr = keyFrame.Attribute("translate");
            if (translateAttr != null && Translate3.TryParse(translateAttr.Value, out Translate3 t))
                nodeInfo.Translate = t;

            return nodeInfo;
        }
    }
}
