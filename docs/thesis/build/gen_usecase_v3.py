"""
Complete use case diagram for IRAS, v3: adds genuine UML <<include>> /
<<extend>> relationships between use cases on top of the zero-crossing v2
layout (Candidate fans left->right; Employer/Admin fan right->left in
disjoint bands). The include/extend links are drawn as bowed dashed
dependency arrows (open arrowhead, per UML convention) routed on the *free*
side of each ellipse column — the side the actor's own fan-lines don't use —
and nested by span (a longer-span link bows further out than a shorter one
whose row-range it overlaps) so no two dependency arrows cross each other
either.
"""
import os
import math
from PIL import Image, ImageDraw
from diagram_helpers import load_font, wrap_text, actor, save, INK, WHITE

OUT = os.path.join(os.path.dirname(__file__), "figures")

CANDIDATE_UC = [
    "UC-01  Register / Login",
    "UC-02  Manage Candidate Profile",
    "UC-03  Upload & Parse Resume",
    "UC-04  Build CV (PDF)",
    "UC-05  Search & Apply for Jobs",
    "UC-06  View Match Score",
    "UC-07  View Skill Gap & Improvement Plan",
    "UC-08  Submit Skill Evidence",
    "UC-09  Attempt Skill Assessment",
    "UC-10  Use Recruitment Chatbot",
    "UC-11  View Notifications",
]
EMPLOYER_UC = [
    "UC-12  Manage Company Profile",
    "UC-13  Generate Job Description (AI)",
    "UC-14  Post / Publish Job",
    "UC-15  Review & Rank Applicants",
    "UC-16  Attach Skill Assessment to Job",
    "UC-17  Schedule Interview",
    "UC-18  Give Candidate Feedback",
]
ADMIN_UC = [
    "UC-19  Manage Users",
    "UC-20  Moderate Job Posts",
    "UC-21  Manage Skill Taxonomy",
    "UC-22  Manage Knowledge Base",
    "UC-23  Review Skill Evidence Decisions",
    "UC-24  Review Audit Log",
    "UC-25  Monitor AI Service Health",
    "UC-26  Generate Reports",
]

ELL_W, ELL_H = 330, 58
PITCH = 78
FILL_CAND = (222, 235, 250)
FILL_EMP = (252, 232, 210)
FILL_ADMIN = (255, 224, 230)
DEP_COLOR = (128, 70, 160)

# (from_index, to_index, kind, bow_depth, condition_or_None) — indices are
# row positions within CANDIDATE_UC / EMPLOYER_UC / ADMIN_UC respectively.
CAND_DEPS = [
    (4, 5, "include", 50, None),                                  # Search&Apply includes View Match Score
    (8, 4, "extend", 110, "job.RequireAssessment = true"),        # Attempt Assessment extends Search&Apply
    (7, 6, "extend", 50, "candidate acts on an identified gap"),  # Submit Evidence extends View Skill Gap
    (3, 1, "include", 78, None),                                  # Build CV includes Manage Profile
]
EMP_DEPS = [
    (2, 1, "include", 50, None),                                   # Post/Publish includes Generate JD (AI)
    (4, 2, "extend", 80, "employer opts into RequireAssessment"),  # Attach Assessment extends Post/Publish
    (6, 3, "include", 112, None),                                  # Give Feedback includes Review & Rank
]
ADMIN_DEPS = [
    (7, 5, "include", 62, None),  # Generate Reports includes Review Audit Log
]


def ellipse_uc(draw, cx, cy, text, fill, font=None):
    draw.ellipse([cx - ELL_W/2, cy - ELL_H/2, cx + ELL_W/2, cy + ELL_H/2], fill=fill, outline=INK, width=2)
    f = font or load_font(12.5)
    lines = wrap_text(draw, text, f, ELL_W - 24)
    ty = cy - (len(lines) * 15) / 2
    for line in lines:
        tw = draw.textlength(line, font=f)
        draw.text((cx - tw/2, ty), line, font=f, fill=INK)
        ty += 15


def fan(draw, actor_pt, ellipse_centers_and_edges, color=(90, 90, 90)):
    for pt in ellipse_centers_and_edges:
        draw.line([actor_pt, pt], fill=color, width=1)


def quad_bezier(p0, p1, p2, t):
    x = (1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0]
    y = (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]
    return (x, y)


def open_arrowhead(dr, tip, direction, size=13, color=DEP_COLOR):
    ang = math.atan2(direction[1], direction[0])
    for da in (0.45, -0.45):
        ax = tip[0] - size * math.cos(ang - da)
        ay = tip[1] - size * math.sin(ang - da)
        dr.line([tip, (ax, ay)], fill=color, width=2)


def draw_dependency(dr, x_col, y_from, y_to, side, kind, bow, condition):
    x_edge = x_col + side * (ELL_W / 2 + 6)
    p0 = (x_edge, y_from)
    p2 = (x_edge, y_to)
    mid_y = (y_from + y_to) / 2
    p1 = (x_col + side * (ELL_W / 2 + bow), mid_y)
    N = 40
    pts = [quad_bezier(p0, p1, p2, t / N) for t in range(N + 1)]
    for i in range(0, N, 2):
        dr.line([pts[i], pts[i + 1]], fill=DEP_COLOR, width=2)
    dx = pts[N][0] - pts[N - 2][0]
    dy = pts[N][1] - pts[N - 2][1]
    open_arrowhead(dr, pts[N], (dx, dy))

    label_lines = [f"«{kind}»"] + ([f"[{condition}]"] if condition else [])
    f_lab = load_font(10, bold=(kind == "include"))
    f_cond = load_font(8.5)
    lx = p1[0] + side * 8
    ly = mid_y - (len(label_lines) * 12) / 2
    max_w = max(dr.textlength(t, font=(f_lab if j == 0 else f_cond)) for j, t in enumerate(label_lines))
    pad = 4
    box_x0 = lx if side > 0 else lx - max_w - 2 * pad
    dr.rectangle([box_x0 - pad, ly - pad, box_x0 + max_w + pad, ly + len(label_lines) * 12 + pad], fill=WHITE)
    for j, t in enumerate(label_lines):
        f = f_lab if j == 0 else f_cond
        dr.text((box_x0, ly + j * 12), t, font=f, fill=DEP_COLOR)


def main():
    n_cand = len(CANDIDATE_UC)
    n_emp = len(EMPLOYER_UC)
    n_admin = len(ADMIN_UC)

    top_margin = 170
    left_col_h = n_cand * PITCH
    right_col_h = (n_emp + n_admin) * PITCH + 70

    canvas_h = top_margin + max(left_col_h, right_col_h) + 190
    canvas_w = 2060

    img = Image.new("RGB", (canvas_w, int(canvas_h)), WHITE)
    dr = ImageDraw.Draw(img)

    f_title = load_font(24, bold=True)
    title = "IRAS — Complete Use Case Diagram with <<include>> / <<extend>> Relationships"
    dr.text((canvas_w/2 - dr.textlength(title, font=f_title)/2, 16), title, font=f_title, fill=INK)
    f_sub = load_font(12)
    sub = "26 use cases, 3 actors + AI Service. Actor associations fan left/right with zero crossings; dashed purple arrows are dependency relationships, bowed and nested so none of them cross each other or an association line."
    dr.text((canvas_w/2 - dr.textlength(sub, font=f_sub)/2, 46), sub, font=f_sub, fill=(100, 100, 100))

    bx0, by0 = 470, top_margin - 10
    bx1, by1 = canvas_w - 420, top_margin - 10 + max(left_col_h, right_col_h) + 40
    dr.rectangle([bx0, by0, bx1, by1], outline=INK, width=2)
    f_bnd = load_font(15, bold=True)
    btitle = "Intelligent Recruitment Automation System"
    dr.text((bx0 + (bx1-bx0)/2 - dr.textlength(btitle, font=f_bnd)/2, by0 + 10), btitle, font=f_bnd, fill=INK)

    # ---- Candidate column ----
    cand_col_x = bx0 + 240
    cand_y0 = by0 + 55
    cand_centers = []
    cand_row_y = []
    for i, label in enumerate(CANDIDATE_UC):
        cy = cand_y0 + i * PITCH + ELL_H/2
        cand_row_y.append(cy)
        ellipse_uc(dr, cand_col_x, cy, label, FILL_CAND)
        cand_centers.append((cand_col_x - ELL_W/2, cy))
    cand_actor_pt = (150, cand_y0 + (n_cand * PITCH)/2 - PITCH/2 + ELL_H/2)
    actor(dr, cand_actor_pt[0] - 12, cand_actor_pt[1] - 55, "Candidate")
    fan(dr, (cand_actor_pt[0] + 12, cand_actor_pt[1] - 20), cand_centers)
    for f_idx, t_idx, kind, bow, cond in CAND_DEPS:
        draw_dependency(dr, cand_col_x, cand_row_y[f_idx], cand_row_y[t_idx], +1, kind, bow, cond)

    # ---- Employer (upper band) + Admin (lower band), shared right column ----
    right_col_x = bx1 - 240
    emp_y0 = by0 + 55
    emp_centers = []
    emp_row_y = []
    for i, label in enumerate(EMPLOYER_UC):
        cy = emp_y0 + i * PITCH + ELL_H/2
        emp_row_y.append(cy)
        ellipse_uc(dr, right_col_x, cy, label, FILL_EMP)
        emp_centers.append((right_col_x + ELL_W/2, cy))
    emp_actor_pt = (canvas_w - 150, emp_y0 + (n_emp * PITCH)/2 - PITCH/2 + ELL_H/2)
    actor(dr, emp_actor_pt[0] - 12, emp_actor_pt[1] - 55, "Employer")
    fan(dr, (emp_actor_pt[0] - 12, emp_actor_pt[1] - 20), emp_centers)
    for f_idx, t_idx, kind, bow, cond in EMP_DEPS:
        draw_dependency(dr, right_col_x, emp_row_y[f_idx], emp_row_y[t_idx], -1, kind, bow, cond)

    admin_y0 = emp_y0 + n_emp * PITCH + 70
    admin_centers = []
    admin_row_y = []
    for i, label in enumerate(ADMIN_UC):
        cy = admin_y0 + i * PITCH + ELL_H/2
        admin_row_y.append(cy)
        ellipse_uc(dr, right_col_x, cy, label, FILL_ADMIN)
        admin_centers.append((right_col_x + ELL_W/2, cy))
    admin_actor_pt = (canvas_w - 150, admin_y0 + (n_admin * PITCH)/2 - PITCH/2 + ELL_H/2)
    actor(dr, admin_actor_pt[0] - 12, admin_actor_pt[1] - 55, "Administrator")
    fan(dr, (admin_actor_pt[0] - 12, admin_actor_pt[1] - 20), admin_centers)
    for f_idx, t_idx, kind, bow, cond in ADMIN_DEPS:
        draw_dependency(dr, right_col_x, admin_row_y[f_idx], admin_row_y[t_idx], -1, kind, bow, cond)

    dr.line([(right_col_x - ELL_W/2 - 20, admin_y0 - 35), (right_col_x + ELL_W/2 + 20, admin_y0 - 35)],
            fill=(200, 200, 200), width=1)

    # ---- legend ----
    lg_x, lg_y = bx0, by1 + 16
    dr.rectangle([lg_x, lg_y, lg_x + 430, lg_y + 74], outline=(120, 120, 120), width=1)
    f_lg = load_font(11, bold=True)
    dr.text((lg_x + 10, lg_y + 6), "Notation:", font=f_lg, fill=INK)
    dr.line([(lg_x + 14, lg_y + 30), (lg_x + 70, lg_y + 30)], fill=(90, 90, 90), width=1)
    dr.text((lg_x + 78, lg_y + 24), "association (actor ↔ use case)", font=load_font(10), fill=INK)
    dr.line([(lg_x + 14, lg_y + 52), (lg_x + 70, lg_y + 52)], fill=DEP_COLOR, width=2)
    open_arrowhead(dr, (lg_x + 70, lg_y + 52), (1, 0), color=DEP_COLOR)
    dr.text((lg_x + 78, lg_y + 46), "«include» / «extend» dependency (arrow = direction)", font=load_font(10), fill=INK)

    note_y = by1 + 16
    f_note_h = load_font(13, bold=True)
    f_note = load_font(11.5)
    nx0 = lg_x + 450
    dr.rectangle([nx0, note_y, nx0 + 1010, note_y + 92], outline=(90, 90, 90), width=1)
    dr.text((nx0 + 10, note_y + 8), "Note — shared precondition (not drawn as a connector to avoid crossing every use case above):",
            font=f_note_h, fill=INK)
    dr.text((nx0 + 10, note_y + 32), "«include» Authenticate (JWT bearer, role claim) is a precondition of every use case in all three actor columns.",
            font=f_note, fill=(60, 60, 60))
    dr.text((nx0 + 10, note_y + 52), "AI Service (supporting, non-human actor) backs: UC-03, UC-05/UC-06, UC-07, UC-08, UC-09, UC-10, UC-13, UC-25",
            font=f_note, fill=(60, 60, 60))
    dr.text((nx0 + 10, note_y + 72), "— see the corresponding sequence diagrams for exactly how each of those calls out to it.",
            font=f_note, fill=(60, 60, 60))

    save(img, os.path.join(OUT, "use_case_diagram_full.png"))
    print("Canvas:", canvas_w, "x", canvas_h, "| Use cases:", n_cand + n_emp + n_admin,
          "| Dependencies:", len(CAND_DEPS) + len(EMP_DEPS) + len(ADMIN_DEPS))


if __name__ == "__main__":
    main()
