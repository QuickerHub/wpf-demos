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
    /// XML parsing and quaternion detection service
    /// </summary>
    public class XmlQuaternionChecker
    {
        /// <summary>
        /// Check quaternions from file path or XML text, automatically detect the input type
        /// </summary>
        /// <param name="input">File path or XML text content</param>
        /// <returns>List of Node names that don't satisfy 90-degree rotation condition</returns>
        public List<string> CheckQuaternionsAuto(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return new List<string>();

            // Check if input is a file path
            if (File.Exists(input))
            {
                return CheckQuaternions(input);
            }

            // Otherwise treat as XML text content
            return CheckQuaternionsFromText(input);
        }

        /// <summary>
        /// Check all Node elements in XML file and return Node names that don't have 90-degree rotation
        /// </summary>
        /// <param name="filePath">Path to XML file</param>
        /// <returns>List of Node names that don't satisfy 90-degree rotation condition</returns>
        public List<string> CheckQuaternions(string filePath)
        {
            var result = new List<string>();
            var nodeInfos = ParseNodes(filePath);

            foreach (var nodeInfo in nodeInfos)
            {
                if (!nodeInfo.Is90DegreeRotation)
                {
                    result.Add(nodeInfo.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// Check all Node elements in XML text and return Node names that don't have 90-degree rotation
        /// </summary>
        /// <param name="xmlText">XML content as string</param>
        /// <returns>List of Node names that don't satisfy 90-degree rotation condition</returns>
        public List<string> CheckQuaternionsFromText(string xmlText)
        {
            var result = new List<string>();
            var nodeInfos = ParseNodesFromText(xmlText);

            foreach (var nodeInfo in nodeInfos)
            {
                if (!nodeInfo.Is90DegreeRotation)
                {
                    result.Add(nodeInfo.Name);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse all Node elements and extract quaternion information
        /// </summary>
        /// <param name="filePath">Path to XML file</param>
        /// <returns>List of NodeInfo objects</returns>
        public List<NodeInfo> ParseNodes(string filePath)
        {
            var result = new List<NodeInfo>();
            var document = XmlHelper.LoadXml(filePath);

            if (!XmlHelper.IsValidXml(document))
                return result;

            // Find all Node elements (they can be at root level or under Tree/Nodes)
            var nodes = document.Descendants("Node").ToList();

            foreach (var node in nodes)
            {
                var nodeInfo = ParseNode(node);
                if (nodeInfo != null)
                {
                    result.Add(nodeInfo);
                }
            }

            return result;
        }

        /// <summary>
        /// Parse all Node elements from XML text and extract quaternion information
        /// </summary>
        /// <param name="xmlText">XML content as string</param>
        /// <returns>List of NodeInfo objects</returns>
        public List<NodeInfo> ParseNodesFromText(string xmlText)
        {
            var result = new List<NodeInfo>();

            if (string.IsNullOrWhiteSpace(xmlText))
                return result;

            try
            {
                var document = XDocument.Parse(xmlText);

                if (!XmlHelper.IsValidXml(document))
                    return result;

                // Find all Node elements (they can be at root level or under Tree/Nodes)
                var nodes = document.Descendants("Node").ToList();

                foreach (var node in nodes)
                {
                    var nodeInfo = ParseNode(node);
                    if (nodeInfo != null)
                    {
                        result.Add(nodeInfo);
                    }
                }
            }
            catch
            {
                // Return empty list if parsing fails
            }

            return result;
        }

        /// <summary>
        /// Parse a single Node element and extract quaternion from KeyFrame
        /// </summary>
        private NodeInfo? ParseNode(XElement node)
        {
            // Get Node name attribute
            var nameAttribute = node.Attribute("name");
            if (nameAttribute == null || string.IsNullOrWhiteSpace(nameAttribute.Value))
                return null;

            var nodeInfo = new NodeInfo
            {
                Name = nameAttribute.Value
            };

            // Find Animation element
            var animation = node.Element("Animation");
            if (animation == null)
                return nodeInfo; // No animation, consider as not 90-degree rotation

            // Find all KeyFrame elements
            var keyFrames = animation.Elements("KeyFrame").ToList();
            if (keyFrames.Count == 0)
                return nodeInfo; // No keyframes, consider as not 90-degree rotation

            // Check each KeyFrame's rotate attribute
            // If any KeyFrame has a rotate attribute, use the first one
            foreach (var keyFrame in keyFrames)
            {
                var rotateAttribute = keyFrame.Attribute("rotate");
                if (rotateAttribute != null && !string.IsNullOrWhiteSpace(rotateAttribute.Value))
                {
                    nodeInfo.RotateString = rotateAttribute.Value;

                    // Parse quaternion
                    if (Quaternion.TryParse(rotateAttribute.Value, out Quaternion quaternion))
                    {
                        nodeInfo.Quaternion = quaternion;
                        nodeInfo.Is90DegreeRotation = quaternion.Is90DegreeRotation();
                    }
                    else
                    {
                        // Failed to parse quaternion, consider as not 90-degree rotation
                        nodeInfo.Is90DegreeRotation = false;
                    }

                    // Use the first valid rotate attribute found
                    break;
                }
            }

            return nodeInfo;
        }
    }
}
