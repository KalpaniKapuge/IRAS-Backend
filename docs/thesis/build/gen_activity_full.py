"""
Complete, whole-system UML Activity Diagram for IRAS with four swimlanes,
ordered CANDIDATE | AI/SYSTEM SERVICES | EMPLOYER | ADMINISTRATOR so that AI
(which exchanges control with the Candidate far more than with anyone else)
sits directly next to it — this is what keeps every cross-lane connector a
short adjacent-lane hop instead of a long diagonal punching through an
unrelated lane's boxes. The employer-hiring branch and the candidate
skill-gap/evidence branch are also placed in non-overlapping row ranges
(both are logically downstream of scoring, but drawing them at the same
rows is what caused the earlier version's crossings) even though in the
real system they can happen concurrently.
"""
import os
import math
from PIL import Image, ImageDraw
from diagram_helpers import load_font, wrap_text, save, INK, WHITE

OUT = os.path.join(os.path.dirname(__file__), "figures")

LANES = ["CANDIDATE", "AI / SYSTEM SERVICES", "EMPLOYER", "ADMINISTRATOR"]
LANE_FILL = [(222, 235, 250), (226, 245, 232), (252, 232, 210), (255, 224, 230)]
LANE_W = 560
PITCH = 118
TOP = 210
LEFT = 40
ACT_W, ACT_H = 460, 74
DEC_W, DEC_H = 260, 90

ACCENT = (37, 84, 199)
LOOP_COLOR = (190, 60, 60)

# node: id -> (lane, row, kind, text)   kind in {start, action, decision, merge, end}
NODES = {
    # ---- Candidate ----
    "C0": (0, 0, "start", "Start"),
    "C1": (0, 1, "action", "Register / Login"),
    "C2": (0, 2, "action", "Build Candidate Profile"),
    "C3": (0, 3, "action", "Upload Resume"),
    "C5": (0, 5, "action", "Review / Correct AI-Extracted Profile"),
    "C6chat": (0, 6, "action", "Use Recruitment Chatbot\n(available at any stage, role-scoped)"),
    "C13": (0, 13, "merge", "Discover Job\n(match notification or search)"),
    "C14": (0, 14, "decision", "Apply for\nthis job?"),
    "C15": (0, 15, "action", "Submit Application"),
    "C19": (0, 19, "action", "Attempt Skill Assessment"),
    "C33": (0, 33, "action", "View Skill Gap; Work Through\nImprovement Plan"),
    "C34": (0, 34, "action", "Submit Skill Evidence"),
    "C38": (0, 38, "merge", "Skill Profile Updated"),
    "C40": (0, 40, "merge", "Receive Notification\n(match / status / feedback)"),
    "C41": (0, 41, "action", "View Feedback & Updated Skill Gap"),
    "CEND": (0, 42, "end", "End — loop: search more jobs /\ncontinue improving skills"),

    # ---- AI / System Services ----
    "AI4": (1, 4, "action", "Parse Resume; Extract Skills (NER)"),
    "AI11": (1, 11, "action", "Run Proactive Matching\n(batched AI ranking)"),
    "AI12": (1, 12, "decision", "Score ≥\nthreshold?"),
    "AI13y": (1, 13, "action", "Create JobMatch; Notify Candidate"),
    "AI14n": (1, 14, "merge", "No notification (below threshold)"),
    "AI16": (1, 16, "action", "Compute Skill Match (local) +\nSemantic Similarity & Fit Score"),
    "AI17": (1, 17, "decision", "Job requires\nassessment?"),
    "AI21": (1, 21, "action", "AI Grades Assessment Answers"),
    "AI22": (1, 22, "action", "Compute Weighted Total Score;\nPersist Application + Skill Gaps"),
    "AI23": (1, 23, "decision", "Skill gap\ndetected?"),
    "AI24": (1, 24, "action", "AI Explains Gap;\nGenerates Improvement Plan"),
    "AI35": (1, 35, "decision", "AI confident\nin evidence?"),
    "AI36": (1, 36, "action", "Auto Approve / Reject Evidence"),

    # ---- Employer ----
    "E0": (2, 0, "start", "Start"),
    "E1": (2, 1, "action", "Register / Login"),
    "E2": (2, 2, "action", "Create Company Profile"),
    "E3": (2, 3, "action", "Input Job Requirements"),
    "E4": (2, 4, "action", "Generate Job Description (AI-Assisted)"),
    "E5": (2, 5, "action", "Review & Edit Draft JD"),
    "E6": (2, 6, "action", "Submit Job for Moderation"),
    "E23": (2, 23, "action", "View Ranked, Explainable Applicant List"),
    "E24": (2, 24, "action", "Compare & Shortlist Candidates"),
    "E25": (2, 25, "action", "Schedule Interview"),
    "E27": (2, 27, "decision", "Hiring\ndecision?"),
    "E28": (2, 28, "action", "Mark Hired; Notify Candidate"),
    "E29": (2, 29, "action", "Mark Rejected"),
    "E30": (2, 30, "action", "Generate AI Feedback Draft"),
    "E31": (2, 31, "action", "Review, Approve & Send Feedback"),

    # ---- Administrator ----
    "A0": (3, 0, "start", "Start"),
    "A1": (3, 1, "action", "Login"),
    "A7": (3, 7, "action", "Review Job Post"),
    "A8": (3, 8, "decision", "Approve\njob post?"),
    "A9r": (3, 9, "action", "Reject with Reason"),
    "A10a": (3, 10, "action", "Approve & Publish Job"),
    "A11": (3, 11, "action", "Manage Users"),
    "A12": (3, 12, "action", "Manage Skill Taxonomy"),
    "A13": (3, 13, "action", "Manage Knowledge Base"),
    "A14": (3, 14, "action", "Monitor AI Service Health"),
    "A15": (3, 15, "action", "Review Audit Log"),
    "A16": (3, 16, "action", "Generate Reports"),
    "A17": (3, 17, "end", "Ongoing — continuous governance cycle"),
    "A36": (3, 36, "action", "Manual Evidence Review & Decision"),
}

# edges: (from, to, label, kind)
# kind: normal (adjacent-lane or same-lane straight line) | loop_left (routed via the
# far-left outer margin, for a same-lane backward loop) | loop_mid (routed via the
# boundary between two specific lanes, for a short backward hop between neighbours)
EDGES = [
    ("C0", "C1", "", "normal"), ("C1", "C2", "", "normal"), ("C2", "C3", "", "normal"),
    ("C3", "AI4", "", "normal"), ("AI4", "C5", "", "normal"), ("C5", "C6chat", "", "normal"),
    ("C6chat", "C13", "", "normal"),
    ("AI13y", "C13", "", "normal"),
    ("C13", "C14", "", "normal"),
    ("C14", "C13", "no — keep browsing", "loop_left"),
    ("C14", "C15", "yes", "normal"),
    ("C15", "AI16", "", "normal"),
    ("AI16", "AI17", "", "normal"),
    ("AI17", "C19", "yes", "normal"),
    ("C19", "AI21", "", "normal"),
    ("AI21", "AI22", "", "normal"),
    ("AI17", "AI22", "no", "normal"),
    ("AI22", "E23", "", "normal"),
    ("AI22", "AI23", "", "normal"),
    ("AI23", "AI24", "yes", "normal"),
    ("AI23", "C40", "no", "normal"),
    ("AI24", "C33", "", "normal"),
    ("C33", "C34", "", "normal"),
    ("C34", "AI35", "", "normal"),
    ("AI35", "AI36", "yes", "normal"),
    ("AI35", "A36", "no", "normal"),
    ("AI36", "C38", "", "normal"),
    ("A36", "C38", "", "normal"),
    ("C38", "C40", "", "normal"),

    ("E0", "E1", "", "normal"), ("E1", "E2", "", "normal"), ("E2", "E3", "", "normal"),
    ("E3", "E4", "", "normal"), ("E4", "E5", "", "normal"), ("E5", "E6", "", "normal"),
    ("E6", "A7", "", "normal"),
    ("E23", "E24", "", "normal"), ("E24", "E25", "", "normal"), ("E25", "E27", "", "normal"),
    ("E27", "E28", "yes", "normal"), ("E27", "E29", "no", "normal"),
    ("E29", "E30", "", "normal"), ("E30", "E31", "", "normal"),
    ("E28", "C40", "", "normal"), ("E31", "C40", "", "normal"),

    ("A0", "A1", "", "normal"), ("A1", "A7", "", "normal"),
    ("A7", "A8", "", "normal"),
    ("A8", "A9r", "no", "normal"), ("A8", "A10a", "yes", "normal"),
    ("A9r", "E6", "revise & resubmit", "loop_mid23"),
    ("A10a", "AI11", "", "loop_mid_admin_ai"),
    ("A10a", "A11", "", "normal"),
    ("A11", "A12", "", "normal"), ("A12", "A13", "", "normal"), ("A13", "A14", "", "normal"),
    ("A14", "A15", "", "normal"), ("A15", "A16", "", "normal"), ("A16", "A17", "", "normal"),

    ("AI11", "AI12", "", "normal"),
    ("AI12", "AI13y", "yes", "normal"), ("AI12", "AI14n", "no", "normal"),

    ("C40", "C41", "", "normal"), ("C41", "CEND", "", "normal"),
    ("CEND", "C13", "continue cycle", "loop_left"),
]


def node_center(node_id):
    lane, row, kind, text = NODES[node_id]
    x = LEFT + lane * LANE_W + LANE_W / 2
    y = TOP + row * PITCH
    return x, y, lane, row, kind, text


def draw_arrowhead(dr, tip, direction, color, size=9):
    ang = math.atan2(direction[1], direction[0])
    for da in (0.5, -0.5):
        ax = tip[0] - size * math.cos(ang - da * 0.7)
        ay = tip[1] - size * math.sin(ang - da * 0.7)
        dr.line([tip, (ax, ay)], fill=color, width=2)


def draw_node(dr, node_id):
    x, y, lane, row, kind, text = node_center(node_id)
    f = load_font(11.5)
    if kind == "start":
        r = 16
        dr.ellipse([x - r, y - r, x + r, y + r], fill=(40, 40, 40))
        return
    if kind == "end":
        r = 18
        dr.ellipse([x - r, y - r, x + r, y + r], outline=(40, 40, 40), width=2)
        dr.ellipse([x - r + 5, y - r + 5, x + r - 5, y + r - 5], fill=(40, 40, 40))
        lines = wrap_text(dr, text, load_font(10), 300)
        ty = y + r + 6
        for line in lines:
            tw = dr.textlength(line, font=load_font(10))
            dr.text((x - tw/2, ty), line, font=load_font(10), fill=(90, 90, 90))
            ty += 13
        return
    if kind == "merge":
        w, h = 300, 50
        dr.ellipse([x - w/2, y - h/2, x + w/2, y + h/2], fill=(240, 240, 245), outline=(110, 110, 120), width=2)
        lines = wrap_text(dr, text, f, w - 24)
        ty = y - (len(lines) * 14) / 2
        for line in lines:
            tw = dr.textlength(line, font=f)
            dr.text((x - tw/2, ty), line, font=f, fill=INK)
            ty += 14
        return
    if kind == "decision":
        w, h = DEC_W, DEC_H
        dr.polygon([(x, y - h/2), (x + w/2, y), (x, y + h/2), (x - w/2, y)],
                   fill=(255, 250, 225), outline=INK, width=2)
        lines = text.split("\n")
        ty = y - (len(lines) * 13) / 2
        for line in lines:
            tw = dr.textlength(line, font=f)
            dr.text((x - tw/2, ty), line, font=f, fill=INK)
            ty += 14
        return
    w, h = ACT_W, ACT_H
    dr.rounded_rectangle([x - w/2, y - h/2, x + w/2, y + h/2], radius=10, fill=WHITE, outline=INK, width=2)
    lines = []
    for part in text.split("\n"):
        lines.extend(wrap_text(dr, part, f, w - 20))
    ty = y - (len(lines) * 15) / 2
    for line in lines:
        tw = dr.textlength(line, font=f)
        dr.text((x - tw/2, ty), line, font=f, fill=INK)
        ty += 15


def node_size(node_id):
    _, _, kind, _ = NODES[node_id]
    if kind == "start":
        return 16, 16
    if kind == "end":
        return 18, 18
    if kind == "merge":
        return 300, 50
    if kind == "decision":
        return DEC_W, DEC_H
    return ACT_W, ACT_H


def edge_point(node_id, toward):
    x, y, lane, row, kind, text = node_center(node_id)
    w, h = node_size(node_id)
    if kind in ("start", "end"):
        r = w / 2
        dx, dy = toward[0] - x, toward[1] - y
        d = math.hypot(dx, dy) or 1
        return (x + dx/d*r, y + dy/d*r)
    dx, dy = toward[0] - x, toward[1] - y
    if dx == 0 and dy == 0:
        return (x, y)
    if abs(dx) * h > abs(dy) * w:
        return (x + (w/2 if dx > 0 else -w/2), y)
    return (x, y + (h/2 if dy > 0 else -h/2))


def main():
    max_row = max(r for _, r, _, _ in NODES.values())
    canvas_w = LEFT * 2 + LANE_W * 4 + 40
    canvas_h = TOP + max_row * PITCH + 160
    img = Image.new("RGB", (canvas_w, canvas_h), WHITE)
    dr = ImageDraw.Draw(img)

    f_title = load_font(26, bold=True)
    title = "IRAS — Complete Activity Diagram (Candidate, AI Service, Employer & Administrator)"
    dr.text((canvas_w/2 - dr.textlength(title, font=f_title)/2, 16), title, font=f_title, fill=INK)
    f_sub = load_font(12.5)
    sub = ("Whole-system flow: registration, AI-assisted job posting & moderation, proactive + reactive matching, assessment gating, "
           "skill-gap/evidence review, interviewing, AI-drafted feedback, and the ongoing admin governance cycle.")
    dr.text((canvas_w/2 - dr.textlength(sub, font=f_sub)/2, 50), sub, font=f_sub, fill=(90, 90, 90))
    f_note = load_font(11)
    note = "Note: the employer hiring branch and the candidate skill-gap/evidence branch are drawn in separate row ranges for legibility; in the running system both can proceed concurrently once scoring completes."
    dr.text((canvas_w/2 - dr.textlength(note, font=f_note)/2, 72), note, font=f_note, fill=(140, 90, 40))

    for i, name in enumerate(LANES):
        x0 = LEFT + i * LANE_W
        dr.rectangle([x0, 120, x0 + LANE_W, canvas_h - 20], outline=(180, 180, 180), width=1)
        dr.rectangle([x0, 120, x0 + LANE_W, 168], fill=LANE_FILL[i], outline=(180, 180, 180), width=1)
        f = load_font(15, bold=True)
        tw = dr.textlength(name, font=f)
        dr.text((x0 + LANE_W/2 - tw/2, 136), name, font=f, fill=INK)

    boundary_23 = LEFT + 3 * LANE_W       # between EMPLOYER(2) and ADMINISTRATOR(3)
    boundary_admin_ai = LEFT + 2 * LANE_W  # between AI(1) and EMPLOYER(2) — used to duck around EMPLOYER

    for a, b, elabel, kind in EDGES:
        ax, ay, *_ = node_center(a)
        bx, by, *_ = node_center(b)
        if kind == "normal":
            p1 = edge_point(a, (bx, by))
            p2 = edge_point(b, (ax, ay))
            dr.line([p1, p2], fill=ACCENT, width=2)
            draw_arrowhead(dr, p2, (p2[0]-p1[0], p2[1]-p1[1]), ACCENT)
            if elabel:
                mx, my = (p1[0]+p2[0])/2, (p1[1]+p2[1])/2
                lw = dr.textlength(elabel, font=load_font(10))
                dr.rectangle([mx-lw/2-3, my-9, mx+lw/2+3, my+9], fill=WHITE)
                col = (140, 90, 20) if elabel in ("yes", "no") else (60, 60, 60)
                dr.text((mx-lw/2, my-7), elabel, font=load_font(10), fill=col)
        elif kind == "loop_left":
            channel_x = LEFT - 22
            p1 = edge_point(a, (channel_x, ay))
            p2 = edge_point(b, (channel_x, by))
            dr.line([p1, (channel_x, p1[1]), (channel_x, p2[1]), p2], fill=LOOP_COLOR, width=2)
            draw_arrowhead(dr, p2, (1, 0.001), LOOP_COLOR)
            lw = dr.textlength(elabel, font=load_font(9.5))
            ly = (p1[1] + p2[1]) / 2
            dr.rectangle([channel_x-lw/2-3, ly-8, channel_x+lw/2+3, ly+8], fill=WHITE)
            dr.text((channel_x-lw/2, ly-6), elabel, font=load_font(9.5), fill=LOOP_COLOR)
        elif kind == "loop_mid23":
            channel_x = boundary_23
            p1 = edge_point(a, (channel_x, ay))
            p2 = edge_point(b, (channel_x, by))
            dr.line([p1, (channel_x, p1[1]), (channel_x, p2[1]), p2], fill=LOOP_COLOR, width=2)
            draw_arrowhead(dr, p2, (-1, 0.001), LOOP_COLOR)
            lw = dr.textlength(elabel, font=load_font(9.5))
            ly = (p1[1] + p2[1]) / 2
            dr.rectangle([channel_x-lw/2-3, ly-8, channel_x+lw/2+3, ly+8], fill=WHITE)
            dr.text((channel_x-lw/2, ly-6), elabel, font=load_font(9.5), fill=LOOP_COLOR)
        elif kind == "loop_mid_admin_ai":
            channel_x = boundary_admin_ai
            p1 = edge_point(a, (channel_x, ay))
            p2 = edge_point(b, (channel_x, by))
            dr.line([p1, (channel_x, p1[1]), (channel_x, p2[1]), p2], fill=ACCENT, width=2)
            draw_arrowhead(dr, p2, (-1, 0.001), ACCENT)

    for node_id in NODES:
        draw_node(dr, node_id)

    save(img, os.path.join(OUT, "activity_diagram_full.png"))
    print("Canvas:", canvas_w, "x", canvas_h, "| Nodes:", len(NODES), "| Edges:", len(EDGES))


if __name__ == "__main__":
    main()
