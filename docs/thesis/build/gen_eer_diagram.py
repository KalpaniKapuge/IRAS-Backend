"""
Enhanced Entity-Relationship (EER) diagram of the IRAS database, built on the
same verified entity/relationship data as gen_full_class_diagram.py so the
two diagrams never drift apart, but rendered in EER/crow's-foot notation:
  - primary keys underlined, foreign keys marked (FK)
  - weak (identity-dependent) entities double-bordered
  - crow's-foot cardinality glyphs instead of UML "1 / * " text
  - an ISA/specialization triangle for User -> {CandidateProfile, EmployerProfile}
"""
import os
import math
from PIL import Image, ImageDraw
from diagram_helpers import load_font, wrap_text, save, INK, WHITE
from gen_full_class_diagram import COLUMNS, EDGES, COL_W, COL_GAP, BOX_GAP, PAD, LEFT_MARGIN, TOP_MARGIN

OUT = os.path.join(os.path.dirname(__file__), "figures")

WEAK_ENTITIES = {"CandidateSkill", "JobRequiredSkill", "CandidateTargetSkill"}
LINE_COLOR = (60, 60, 70)
ISA_COLOR = (155, 45, 130)

CARD_TO_GLYPH = {"1": "one", "0..1": "zero_one", "*": "many", "0..*": "many"}

FONT_TITLE = load_font(13, bold=True)
FONT_ATTR = load_font(9.5)


def compute_box_height(pk_lines, attr_lines):
    return PAD * 2 + 20 + 4 + len(pk_lines) * 14 + 3 + len(attr_lines) * 13.5 + 6


# Regular associations rendered with crow's-foot glyphs (the two ISA edges are
# pulled out and drawn separately as a specialization triangle instead).
ISA_EDGES = {("User", "CandidateProfile"), ("User", "EmployerProfile")}
CROWFOOT_EDGES = [e for e in EDGES if (e[0], e[1]) not in ISA_EDGES]


def draw_marker(draw, point, out_dir, kind, color=LINE_COLOR):
    """Draw a crow's-foot cardinality glyph at `point`, opening away from the
    entity along unit vector `out_dir` (pointing from the entity outward,
    i.e. along the connector toward the other entity)."""
    dx, dy = out_dir
    px, py = -dy, dx  # perpendicular unit vector
    bar_half = 7

    def bar_at(dist):
        cx, cy = point[0] + dx * dist, point[1] + dy * dist
        draw.line([(cx - px * bar_half, cy - py * bar_half),
                   (cx + px * bar_half, cy + py * bar_half)], fill=color, width=2)

    def circle_at(dist, r=5):
        cx, cy = point[0] + dx * dist, point[1] + dy * dist
        draw.ellipse([cx - r, cy - r, cx + r, cy + r], outline=color, width=2, fill=WHITE)

    def crowfoot_at():
        tip = point
        base_dist = 16
        bx, by = point[0] + dx * base_dist, point[1] + dy * base_dist
        for off in (-1, 0, 1):
            ex = bx + px * off * bar_half * 1.4
            ey = by + py * off * bar_half * 1.4
            draw.line([tip, (ex, ey)], fill=color, width=2)

    if kind == "one":
        bar_at(9)
    elif kind == "zero_one":
        circle_at(8)
        bar_at(18)
    elif kind == "many":
        circle_at(8)
        crowfoot_at()


def edge_endpoints(a_box, b_box, same_col):
    ax, ay, aw, ah = a_box
    bx, by, bw, bh = b_box
    if same_col:
        y1 = ay + ah * 0.5
        y2 = by + bh * 0.5
        x = ax + aw
        return (x, y1), (x, y2), "loop"
    y1 = ay + ah * 0.5
    y2 = by + bh * 0.5
    if bx > ax:
        return (ax + aw, y1), (bx, y2), "fwd"
    else:
        return (ax, y1), (bx + bw, y2), "back"


def main():
    registry = {}
    col_x = LEFT_MARGIN
    col_heights = []
    layout = []
    for ci, (header, color, classes) in enumerate(COLUMNS):
        y = TOP_MARGIN
        boxes = []
        for name, pk_lines, attr_lines in classes:
            h = compute_box_height(pk_lines, attr_lines)
            boxes.append((name, pk_lines, attr_lines, y, h))
            registry[name] = (col_x, y, COL_W, h, ci)
            y += h + BOX_GAP
        col_heights.append(y)
        layout.append((ci, col_x, header, color, boxes))
        col_x += COL_W + COL_GAP

    canvas_w = col_x - COL_GAP + LEFT_MARGIN
    canvas_h = max(col_heights) + 60
    img = Image.new("RGB", (int(canvas_w), int(canvas_h)), WHITE)
    dr = ImageDraw.Draw(img)

    f_title = load_font(26, bold=True)
    title = "IRAS — Enhanced Entity-Relationship (EER) Diagram — Full Schema (36 Entities)"
    dr.text((canvas_w/2 - dr.textlength(title, font=f_title)/2, 16), title, font=f_title, fill=INK)
    f_sub = load_font(12.5)
    sub = ("Crow's-foot cardinality; primary keys underlined; double border = weak/associative entity "
           "(identity fully derived from its two foreign keys); triangle = ISA specialization")
    dr.text((canvas_w/2 - dr.textlength(sub, font=f_sub)/2, 48), sub, font=f_sub, fill=(90, 90, 90))

    # ---- Legend ----
    lg_x, lg_y = canvas_w - 760, 14
    dr.rectangle([lg_x, lg_y, lg_x + 750, lg_y + 96], outline=INK, width=1)
    f_lg = load_font(11)
    dr.text((lg_x + 10, lg_y + 4), "Notation:", font=load_font(11, bold=True), fill=INK)
    legend_items = [
        ("one (mandatory)", "one"), ("zero or one", "zero_one"), ("zero or many", "many"),
    ]
    lx = lg_x + 100
    for label, kind in legend_items:
        ly = lg_y + 22
        draw_marker(dr, (lx + 40, ly), (1, 0), kind)
        dr.line([(lx, ly), (lx + 40, ly)], fill=LINE_COLOR, width=2)
        dr.text((lx + 46, ly - 6), label, font=f_lg, fill=INK)
        lx += 220
    dr.text((lg_x + 10, lg_y + 46), "PK attribute", font=f_lg, fill=(150, 40, 40))
    dr.line([(lg_x + 100, lg_y + 56), (lg_x + 160, lg_y + 56)], fill=(150, 40, 40), width=1)
    dr.text((lg_x + 170, lg_y + 46), "(underlined = primary key)", font=f_lg, fill=(90, 90, 90))
    dr.rectangle([lg_x + 10, lg_y + 66, lg_x + 34, lg_y + 84], outline=INK, width=1)
    dr.rectangle([lg_x + 13, lg_y + 69, lg_x + 31, lg_y + 81], outline=INK, width=1)
    dr.text((lg_x + 44, lg_y + 68), "weak / associative entity (composite PK)", font=f_lg, fill=INK)

    # ---- Column headers ----
    for ci, cx, header, color, boxes in layout:
        dr.rectangle([cx, 122, cx + COL_W, 172], fill=color, outline=INK, width=2)
        f = load_font(14, bold=True)
        for j, line in enumerate(wrap_text(dr, header, f, COL_W - 16)):
            tw = dr.textlength(line, font=f)
            dr.text((cx + COL_W/2 - tw/2, 130 + j*18), line, font=f, fill=INK)

    # ---- Crow's-foot relationship connectors (drawn under boxes) ----
    for a, b, ca, cb in CROWFOOT_EDGES:
        if a not in registry or b not in registry:
            continue
        abox = registry[a][:4]
        bbox = registry[b][:4]
        same_col = registry[a][4] == registry[b][4]
        p1, p2, mode = edge_endpoints(abox, bbox, same_col)
        if mode == "loop":
            x = p1[0]
            dr.line([p1, (x + 26, p1[1]), (x + 26, p2[1]), p2], fill=LINE_COLOR, width=1)
            draw_marker(dr, p1, (1, 0), CARD_TO_GLYPH[ca])
            draw_marker(dr, p2, (1, 0), CARD_TO_GLYPH[cb])
        else:
            dr.line([p1, p2], fill=LINE_COLOR, width=1)
            dirvec = (1, 0) if mode == "fwd" else (-1, 0)
            draw_marker(dr, p1, dirvec, CARD_TO_GLYPH[ca])
            draw_marker(dr, p2, (-dirvec[0], 0), CARD_TO_GLYPH[cb])

    # ---- Entity boxes ----
    for ci, cx, header, color, boxes in layout:
        for name, pk_lines, attr_lines, y, h in boxes:
            x = cx
            is_weak = name in WEAK_ENTITIES
            dr.rectangle([x, y, x + COL_W, y + h], fill=WHITE, outline=INK, width=2)
            if is_weak:
                dr.rectangle([x + 4, y + 4, x + COL_W - 4, y + h - 4], outline=INK, width=1)
            ty = y + PAD
            tw = dr.textlength(name, font=FONT_TITLE)
            dr.text((x + COL_W/2 - tw/2, ty), name, font=FONT_TITLE, fill=INK)
            ty += 20
            dr.line([(x, ty), (x + COL_W, ty)], fill=INK, width=1)
            ty += 4
            for line in pk_lines:
                dr.text((x + PAD, ty), line, font=FONT_ATTR, fill=(150, 40, 40))
                tw2 = dr.textlength(line, font=FONT_ATTR)
                dr.line([(x + PAD, ty + 12), (x + PAD + tw2, ty + 12)], fill=(150, 40, 40), width=1)
                ty += 14
            ty += 3
            dr.line([(x + 4, ty - 2), (x + COL_W - 4, ty - 2)], fill=(210, 210, 210), width=1)
            for line in attr_lines:
                dr.text((x + PAD, ty), line, font=FONT_ATTR, fill=INK)
                ty += 13.5

    # ---- ISA / specialization triangle: User -> {CandidateProfile, EmployerProfile} ----
    # Drawn last so it always renders on top of the boxes/lines it sits between.
    ux, uy, uw, uh, _ = registry["User"]
    cx1, cy1, cw1, ch1, _ = registry["CandidateProfile"]
    ex1, ey1, ew1, eh1, _ = registry["EmployerProfile"]
    tri_r = 11
    tri_x = ux + uw + COL_GAP / 2
    tri_y = uy + uh * 0.5
    dr.line([(ux + uw, tri_y), (tri_x - tri_r, tri_y)], fill=ISA_COLOR, width=2)
    tri = [(tri_x - tri_r, tri_y - tri_r), (tri_x - tri_r, tri_y + tri_r), (tri_x + tri_r, tri_y)]
    dr.polygon(tri, outline=ISA_COLOR, width=2, fill=(245, 232, 242))
    f_isa = load_font(10, bold=True)
    dr.text((tri_x - 4, tri_y - 6), "d", font=f_isa, fill=ISA_COLOR)
    dr.text((tri_x - 22, tri_y - 30), "ISA", font=f_isa, fill=ISA_COLOR)
    dr.text((tri_x - 34, tri_y + 16), "(disjoint,", font=load_font(8), fill=ISA_COLOR)
    dr.text((tri_x - 34, tri_y + 26), "partial)", font=load_font(8), fill=ISA_COLOR)
    # Physical line only to the adjacent subclass (CandidateProfile); EmployerProfile
    # sits three columns away and a full-width connector there would cross straight
    # through two unrelated swimlanes, so it is annotated locally on its own box instead.
    p_cand = (cx1, cy1 + ch1 * 0.5)
    mid_x1 = cx1 - COL_GAP / 2
    dr.line([(tri_x + tri_r, tri_y), (mid_x1, tri_y), (mid_x1, p_cand[1]), p_cand], fill=ISA_COLOR, width=2)
    draw_marker(dr, p_cand, (1, 0), "one", color=ISA_COLOR)

    f_isa_tag = load_font(9, bold=True)
    tag = "◁ ISA subclass of User (disjoint, partial — see IDENTITY swimlane)"
    dr.text((ex1 + 8, ey1 - 14), tag, font=f_isa_tag, fill=ISA_COLOR)

    save(img, os.path.join(OUT, "eer_diagram_full.png"))
    print("Canvas size:", canvas_w, "x", canvas_h)
    print("Entities:", sum(len(c) for _, _, c in COLUMNS), "| Crow's-foot edges:", len(CROWFOOT_EDGES), "| ISA edges: 2")


if __name__ == "__main__":
    main()
