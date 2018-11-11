==================================================
NodeHelper
==================================================

NodeHelper is a plugin for Kerbal Space Program (KSP) that allows part attachment node editing in real time. You can
also attach other parts to those nodes and check how it looks. Any attached parts can be moved along with the nodes.

In order to move a node:

    • Set the step width value via either the step width buttons or enter the step width value directly.
    • Use the positioning buttons to move the node in the respective direction or enter a position (relative to the
      center of the part the node belongs to) and set the node directly to this position.
    • Save the node position. You can reset to the last position at any time. However, if you update the reset
      position then the current position will be used from now on and cannot be undone.

Do note the NodeHelper only works for stack attachment nodes and not surface ones. A workaround exists for surface nodes
where you can create a normal stack node, move it at the desired position and then manually rename the node type from
"stack" to "srf".

NodeHelper can also edit the attachment node rules, sizes and orientation (angle).

Fair warning: existing moved and/or deleted attachment nodes are handled fine but newly created ones (not saved yet in
the part configuration file) are not. This is a limitation of how KSP handles attachment node physics.

Installation:

Unzip the downloaded .zip file and merge it with your GameData folder. If you are updating then make sure that you
have completely removed any previous NodeHelper installations beforehand.

Credits:

    • marce for creating the NodeHelper mod.
    • Felbourn for maintaining it in the absence of marce (KSP 1.0 branch compatibility, bug fixes and new features).

License:

NodeHelper is licensed under a Creative Commons Attribution-NonCommercial-ShareAlike 4.0 (CC-BY-NC-SA 4.0) license.

You should have received a copy of the license along with this work. If not, visit the official Creative Commons web page: https://creativecommons.org/licenses/by-nc-sa/4.0/legalcode

Note that the above license does not cover mod packs. Redistributing this work via a mod pack is not allowed.
