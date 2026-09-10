"""
Complete, zero-crossing use case diagram for IRAS.

Design to avoid any line/shape collision:
  - Candidate fans LEFT->RIGHT into its own column (no other actor shares its Y-range).
  - Employer and Admin both fan RIGHT->LEFT into a second column, but occupy disjoint
    Y-bands (Employer above, Admin below) so their fans never overlap each other.
  - A shared "Authenticate" use case and the AI Service's supporting role are recorded
    as text notes, not as connector lines, since a line from a corner actor to a dozen
    scattered use cases is exactly the kind of crossing the request asked to avoid.
"""
import os
from diagram_helpers import load_font, wrap_text, actor, save, INK, WHITE, ACCENT

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


def main():
    n_cand = len(CANDIDATE_UC)
    n_emp = len(EMPLOYER_UC)
    n_admin = len(ADMIN_UC)

    top_margin = 170
    left_col_h = n_cand * PITCH
    right_col_h = (n_emp + n_admin) * PITCH + 70  # extra gap between the two right-side groups

    canvas_h = top_margin + max(left_col_h, right_col_h) + 140
    canvas_w = 1980

    from PIL import Image as PILImage, ImageDraw
    img = PILImage.new("RGB", (canvas_w, int(canvas_h)), WHITE)
    dr = ImageDraw.Draw(img)

    f_title = load_font(24, bold=True)
    title = "IRAS — Complete Use Case Diagram (26 Use Cases, 3 Actors + AI Service)"
    dr.text((canvas_w/2 - dr.textlength(title, font=f_title)/2, 16), title, font=f_title, fill=INK)
    f_sub = load_font(12)
    sub = "Candidate fans left→right; Employer and Admin fan right→left in separate, non-overlapping bands — no two association lines cross anywhere in this diagram."
    dr.text((canvas_w/2 - dr.textlength(sub, font=f_sub)/2, 46), sub, font=f_sub, fill=(100, 100, 100))

    # System boundary
    bx0, by0 = 430, top_margin - 10
    bx1, by1 = canvas_w - 380, top_margin - 10 + max(left_col_h, right_col_h) + 40
    dr.rectangle([bx0, by0, bx1, by1], outline=INK, width=2)
    f_bnd = load_font(15, bold=True)
    btitle = "Intelligent Recruitment Automation System"
    dr.text((bx0 + (bx1-bx0)/2 - dr.textlength(btitle, font=f_bnd)/2, by0 + 10), btitle, font=f_bnd, fill=INK)

    # ---- Candidate column (left, fans left->right) ----
    cand_col_x = bx0 + 220
    cand_y0 = by0 + 55
    cand_centers = []
    for i, label in enumerate(CANDIDATE_UC):
        cy = cand_y0 + i * PITCH + ELL_H/2
        ellipse_uc(dr, cand_col_x, cy, label, FILL_CAND)
        cand_centers.append((cand_col_x - ELL_W/2, cy))
    cand_actor_pt = (150, cand_y0 + (n_cand * PITCH)/2 - PITCH/2 + ELL_H/2)
    bottom_label = actor(dr, cand_actor_pt[0] - 12, cand_actor_pt[1] - 55, "Candidate")
    fan(dr, (cand_actor_pt[0] + 12, cand_actor_pt[1] - 20), cand_centers)

    # ---- Right column: Employer (upper band) then Admin (lower band), both fan right->left ----
    right_col_x = bx1 - 220
    emp_y0 = by0 + 55
    emp_centers = []
    for i, label in enumerate(EMPLOYER_UC):
        cy = emp_y0 + i * PITCH + ELL_H/2
        ellipse_uc(dr, right_col_x, cy, label, FILL_EMP)
        emp_centers.append((right_col_x + ELL_W/2, cy))
    emp_actor_pt = (canvas_w - 150, emp_y0 + (n_emp * PITCH)/2 - PITCH/2 + ELL_H/2)
    actor(dr, emp_actor_pt[0] - 12, emp_actor_pt[1] - 55, "Employer")
    fan(dr, (emp_actor_pt[0] - 12, emp_actor_pt[1] - 20), emp_centers)

    admin_y0 = emp_y0 + n_emp * PITCH + 70
    admin_centers = []
    for i, label in enumerate(ADMIN_UC):
        cy = admin_y0 + i * PITCH + ELL_H/2
        ellipse_uc(dr, right_col_x, cy, label, FILL_ADMIN)
        admin_centers.append((right_col_x + ELL_W/2, cy))
    admin_actor_pt = (canvas_w - 150, admin_y0 + (n_admin * PITCH)/2 - PITCH/2 + ELL_H/2)
    actor(dr, admin_actor_pt[0] - 12, admin_actor_pt[1] - 55, "Administrator")
    fan(dr, (admin_actor_pt[0] - 12, admin_actor_pt[1] - 20), admin_centers)

    # divider between employer and admin bands (visual separation, not a connector)
    dr.line([(right_col_x - ELL_W/2 - 20, admin_y0 - 35), (right_col_x + ELL_W/2 + 20, admin_y0 - 35)],
            fill=(200, 200, 200), width=1)

    # ---- Notes instead of long crossing connectors ----
    note_y = by1 + 20
    f_note_h = load_font(13, bold=True)
    f_note = load_font(11.5)
    dr.rectangle([bx0, note_y, bx0 + 900, note_y + 92], outline=(90, 90, 90), width=1)
    dr.text((bx0 + 10, note_y + 8), "Note — shared precondition (not drawn as a connector to avoid crossing every use case above):",
            font=f_note_h, fill=INK)
    dr.text((bx0 + 10, note_y + 32), "«include» Authenticate (JWT bearer, role claim) is a precondition of every use case in all three actor columns.",
            font=f_note, fill=(60, 60, 60))
    dr.text((bx0 + 10, note_y + 52), "AI Service (supporting, non-human actor) backs: UC-03, UC-05/UC-06, UC-07, UC-08, UC-09, UC-10, UC-13, UC-25",
            font=f_note, fill=(60, 60, 60))
    dr.text((bx0 + 10, note_y + 72), "— see the corresponding sequence diagrams for exactly how each of those calls out to it.",
            font=f_note, fill=(60, 60, 60))

    save(img, os.path.join(OUT, "use_case_diagram_full.png"))
    print("Canvas:", canvas_w, "x", canvas_h, "| Use cases:", n_cand + n_emp + n_admin)


if __name__ == "__main__":
    main()
