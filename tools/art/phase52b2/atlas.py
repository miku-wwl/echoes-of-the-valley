"""Shared UV atlas layout for the caretaker textures.

`design.py` maps every surface into one of these rectangles and `textures.py` paints into the same rectangles, so the two
never need to read each other's output. A rectangle is (u0, v0, u1, v1) in Unity texture space (v = 1 is the TOP of the PNG).
A rectangle whose v0 > v1 runs top-to-bottom: row 0 of a grid lands at the top of the painted region.
"""

# Texture sizes (pixels) per material.
SIZES = {
    'Skin': (1024, 1024),
    'Hair': (1024, 1024),
    'Teal': (1024, 1024),
    'Cream': (1024, 1024),
    'Burgundy': (512, 512),
    'Leather': (1024, 1024),
    'Tights': (256, 512),
    'Trinket': (128, 128),
}

# Skin: the whole sheet is the planar front projection of the head (u = 0.5 + x/0.20, v = 0.5 + (y - 1.535)/0.28).
# A flat-skin patch in the top-right corner serves the neck, hands, ears and the back of the head.
SKIN_FLAT = (0.945, 0.945)
FACE_U_SCALE = 0.20
FACE_V_SCALE = 0.28
FACE_CENTER_Y = 1.535

# Hair: left half = clump strip (u across the clump, v along it, root at v = 1 and tip at v = 0), right half = scalp cap.
HAIR = {
    'lock': (0.0, 1.0, 0.5, 0.0),
    'cap': (0.5, 1.0, 1.0, 0.0),
}

# Teal coat. v = 1 at the top of the PNG.
TEAL = {
    'coat': (0.0, 1.0, 0.64, 0.30),        # front-edge-to-front-edge sheet, collar row at the top, hem row at the bottom
    'sleeve': (0.66, 1.0, 1.0, 0.50),      # outside of the wide sleeve, shoulder at the top, cuff at the bottom
    'lining': (0.66, 0.48, 1.0, 0.30),     # cream-teal lining for the inner shells
    'trim': (0.0, 0.28, 1.0, 0.0),         # plain strip used for edge rows
}

# Cream: blouse, skirt, petticoat frill.
CREAM = {
    'blouse': (0.0, 1.0, 0.5, 0.5),
    'skirt': (0.5, 1.0, 1.0, 0.5),
    'skirt_in': (0.0, 0.5, 0.5, 0.0),
    'frill': (0.5, 0.5, 1.0, 0.0),
}

# Burgundy: scarf collar, scarf tails (fringe at the bottom of the tail region), petticoat.
BURGUNDY = {
    'collar': (0.0, 1.0, 1.0, 0.62),
    'tail': (0.0, 0.60, 0.5, 0.0),
    'petticoat': (0.5, 0.60, 1.0, 0.0),
}

# Leather: belt/corset band, straps, boot shaft, boot foot, cuff, sole, satchel body, satchel flap.
LEATHER = {
    'corset': (0.0, 1.0, 0.5, 0.78),
    'strap': (0.5, 1.0, 1.0, 0.90),
    'strap2': (0.5, 0.88, 1.0, 0.78),
    'boot': (0.0, 0.76, 0.5, 0.40),
    'cuff': (0.5, 0.76, 1.0, 0.62),
    'sole': (0.5, 0.60, 1.0, 0.52),
    'bag': (0.0, 0.38, 0.5, 0.0),
    'flap': (0.5, 0.50, 1.0, 0.0),
}

TIGHTS = {'leg': (0.0, 1.0, 1.0, 0.0)}

# Trinket: a flat white texel block; colour comes from vertex colours.
TRINKET = {'flat': (0.45, 0.55, 0.55, 0.45)}

# Preview / fallback base colours (linear-ish sRGB hex) per material.
PALETTE = {
    'Skin': 'F1C4A8', 'Hair': '7A4E34', 'Teal': '2D6A66', 'Cream': 'F2E4C6', 'Burgundy': '7E3347',
    'Leather': '6A4631', 'Tights': '45332C', 'Trinket': 'C8A45A',
}

# Solid-colour texels used by the thin edge strips of cloth and leather (centre of a painted block).
EDGE = {
    'Teal': (0.97, 0.13),      # gold piping
    'Cream': (0.97, 0.03),
    'Burgundy': (0.97, 0.03),
    'Leather': (0.97, 0.03),   # dark edge paint
}
