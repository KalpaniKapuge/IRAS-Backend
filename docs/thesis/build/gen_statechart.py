"""
UML state machine (state chart) diagrams for the two entities in IRAS with
enough lifecycle complexity to warrant one: Application and SkillPlanEvidence
(Section 4.4.5). Both state machines are enforced at the application-service
layer, not left to the client — the diagram exists to make that enforced
transition set explicit and auditable.
"""
import os
import math
from PIL import Image, ImageDraw
from diagram_helpers import load_font, wrap_text, save, INK, WHITE, ACCENT

OUT = os.path.join(os.path.dirname(__file__), "figures")
END_COLOR = (150, 40, 40)
GUARD_COLOR = (100, 100, 100)


def state_box(dr, cx, cy, w, h, title, fill=(234, 240, 254)):
    dr.rounded_rectangle([cx - w/2, cy - h/2, cx + w/2, cy + h/2], radius=14, fill=fill, outline=INK, width=2)
    f = load_font(13, bold=True)
    tw = dr.textlength(title, font=f)
    dr.text((cx - tw/2, cy - 8), title, font=f, fill=INK)
    return (cx, cy, w, h)


def start_state(dr, cx, cy, r=11):
    dr.ellipse([cx - r, cy - r, cx + r, cy + r], fill=(30, 30, 30))
    return (cx, cy)


def end_state(dr, cx, cy, r_out=16, r_in=9, label=""):
    dr.ellipse([cx - r_out, cy - r_out, cx + r_out, cy + r_out], outline=END_COLOR, width=2, fill=WHITE)
    dr.ellipse([cx - r_in, cy - r_in, cx + r_in, cy + r_in], fill=END_COLOR)
    f = load_font(12.5, bold=True)
    tw = dr.textlength(label, font=f)
    dr.text((cx - tw/2, cy + r_out + 6), label, font=f, fill=END_COLOR)
    return (cx, cy, r_out)


def arrowhead(dr, tip, direction, color=ACCENT, size=10):
    ang = math.atan2(direction[1], direction[0])
    for da in (0.5, -0.5):
        ax = tip[0] - size * math.cos(ang - da)
        ay = tip[1] - size * math.sin(ang - da)
        dr.line([tip, (ax, ay)], fill=color, width=2)


def edge(dr, p1, p2, label=None, color=ACCENT, label_pos=0.5, dashed=False):
    if dashed:
        dist = math.hypot(p2[0]-p1[0], p2[1]-p1[1])
        n = max(int(dist / 12), 1)
        for i in range(n):
            if i % 2 == 0:
                a = (p1[0] + (p2[0]-p1[0])*i/n, p1[1] + (p2[1]-p1[1])*i/n)
                b = (p1[0] + (p2[0]-p1[0])*(i+1)/n, p1[1] + (p2[1]-p1[1])*(i+1)/n)
                dr.line([a, b], fill=color, width=2)
    else:
        dr.line([p1, p2], fill=color, width=2)
    arrowhead(dr, p2, (p2[0]-p1[0], p2[1]-p1[1]), color=color)
    if label:
        f = load_font(11)
        mx = p1[0] + (p2[0]-p1[0]) * label_pos
        my = p1[1] + (p2[1]-p1[1]) * label_pos
        tw = dr.textlength(label, font=f)
        dr.rectangle([mx - tw/2 - 3, my - 9, mx + tw/2 + 3, my + 9], fill=WHITE)
        dr.text((mx - tw/2, my - 7), label, font=f, fill=GUARD_COLOR)


def polyline(dr, pts, label=None, color=ACCENT, label_idx=1):
    for i in range(len(pts) - 1):
        dr.line([pts[i], pts[i+1]], fill=color, width=2)
    arrowhead(dr, pts[-1], (pts[-1][0]-pts[-2][0], pts[-1][1]-pts[-2][1]), color=color)
    if label:
        f = load_font(11)
        mx, my = pts[label_idx]
        tw = dr.textlength(label, font=f)
        dr.rectangle([mx - tw/2 - 3, my - 9, mx + tw/2 + 3, my + 9], fill=WHITE)
        dr.text((mx - tw/2, my - 7), label, font=f, fill=GUARD_COLOR)


def main():
    W, H = 1500, 1500
    img = Image.new("RGB", (W, H), WHITE)
    dr = ImageDraw.Draw(img)

    f_title = load_font(24, bold=True)
    title = "IRAS — State Chart Diagrams: Application and SkillPlanEvidence"
    dr.text((W/2 - dr.textlength(title, font=f_title)/2, 14), title, font=f_title, fill=INK)
    f_sub = load_font(12)
    sub = "Both state machines are enforced in ApplicationService / SkillPlanEvidenceService — no client request can move an entity outside these transitions."
    dr.text((W/2 - dr.textlength(sub, font=f_sub)/2, 46), sub, font=f_sub, fill=(100, 100, 100))

    # ============================================================ PANEL 1: Application
    p1_y = 100
    dr.rectangle([40, p1_y, W - 40, p1_y + 560], outline=(180, 180, 180), width=1)
    f_h = load_font(15, bold=True)
    dr.text((60, p1_y + 10), "Application  (ApplicationStatus)", font=f_h, fill=INK)

    spine_y = p1_y + 130
    sx = start_state(dr, 100, spine_y)
    applied = state_box(dr, 230, spine_y, 150, 64, "Applied")
    screened = state_box(dr, 430, spine_y, 150, 64, "Screened")
    shortlisted = state_box(dr, 630, spine_y, 170, 64, "Shortlisted")
    interview = state_box(dr, 850, spine_y, 150, 64, "Interview")
    hired = end_state(dr, 1040, spine_y, label="Hired")

    edge(dr, (sx[0]+11, sx[1]), (applied[0]-75, applied[1]))
    edge(dr, (applied[0]+75, applied[1]), (screened[0]-75, screened[1]), "screen")
    edge(dr, (screened[0]+75, screened[1]), (shortlisted[0]-85, shortlisted[1]), "shortlist")
    edge(dr, (shortlisted[0]+85, shortlisted[1]), (interview[0]-75, interview[1]), "schedule interview")
    edge(dr, (interview[0]+75, interview[1]), (hired[0]-hired[2], hired[1]), "hire")

    # collector line: any of Applied/Screened/Shortlisted/Interview -> reject or withdraw
    collector_y = spine_y + 130
    for box in (applied, screened, shortlisted, interview):
        edge(dr, (box[0], box[1]+32), (box[0], collector_y), color=(150, 150, 160))
    dr.line([(applied[0], collector_y), (interview[0], collector_y)], fill=(150, 150, 160), width=2)
    trunk_x = (applied[0] + interview[0]) / 2
    trunk_bottom = collector_y + 60
    dr.line([(trunk_x, collector_y), (trunk_x, trunk_bottom)], fill=(150, 150, 160), width=2)

    withdrawn = end_state(dr, trunk_x - 220, trunk_bottom + 70, label="Withdrawn")
    rejected = end_state(dr, trunk_x + 220, trunk_bottom + 70, label="Rejected")
    polyline(dr, [(trunk_x, trunk_bottom), (trunk_x - 220, trunk_bottom), (withdrawn[0], withdrawn[1]-withdrawn[2])],
             label="withdraw", color=(150, 150, 160), label_idx=1)
    polyline(dr, [(trunk_x, trunk_bottom), (trunk_x + 220, trunk_bottom), (rejected[0], rejected[1]-rejected[2])],
             label="reject", color=(150, 150, 160), label_idx=1)

    f_note = load_font(10.5)
    note1 = "Any of Applied / Screened / Shortlisted / Interview -> Withdrawn (candidate action) or Rejected (employer decision)."
    dr.text((60, trunk_bottom + 130), note1, font=f_note, fill=(90, 90, 90))
    note2 = "Hired is reachable only from Interview — enforced server-side, not just hidden in the UI."
    dr.text((60, trunk_bottom + 150), note2, font=f_note, fill=(90, 90, 90))

    # ============================================================ PANEL 2: SkillPlanEvidence
    p2_y = p1_y + 610
    dr.rectangle([40, p2_y, W - 40, p2_y + 560], outline=(180, 180, 180), width=1)
    dr.text((60, p2_y + 10), "SkillPlanEvidence  (EvidenceVerificationStatus)", font=f_h, fill=INK)

    spine2_y = p2_y + 220
    sx2 = start_state(dr, 100, spine2_y)
    draft = state_box(dr, 250, spine2_y, 150, 64, "Draft")
    pending = state_box(dr, 470, spine2_y, 150, 64, "Pending")
    revision = state_box(dr, 760, spine2_y, 190, 64, "RevisionRequired", fill=(255, 240, 220))
    approved = end_state(dr, 1030, spine2_y - 140, label="Approved")
    rejected2 = end_state(dr, 1030, spine2_y + 140, label="Rejected")

    edge(dr, (sx2[0]+11, sx2[1]), (draft[0]-75, draft[1]))
    edge(dr, (draft[0]+75, draft[1]), (pending[0]-75, pending[1]), "submit for review")
    edge(dr, (pending[0]+75, pending[1]-8), (approved[0]-approved[2]*0.7, approved[1]+approved[2]*0.7), "AI/admin approve")
    edge(dr, (pending[0]+75, pending[1]+8), (rejected2[0]-rejected2[2]*0.7, rejected2[1]-rejected2[2]*0.7), "AI/admin reject")
    edge(dr, (pending[0]+75, pending[1]), (revision[0]-95, revision[1]), "AI/admin: needs more evidence")

    # loop-back channels, routed on opposite sides so they never cross the forward spine or each other
    top_channel_y = spine2_y - 90
    polyline(dr, [(pending[0], pending[1]-32), (pending[0], top_channel_y), (draft[0], top_channel_y), (draft[0], draft[1]-32)],
             label="withdraw before decision", color=(150, 150, 160), label_idx=2)

    bottom_channel_y = spine2_y + 90
    polyline(dr, [(revision[0], revision[1]+32), (revision[0], bottom_channel_y), (draft[0], bottom_channel_y), (draft[0], draft[1]+32)],
             label="revise & resubmit", color=(150, 150, 160), label_idx=2)

    # ============================================================ legend
    lg_y = p2_y + 470
    f_lg = load_font(11, bold=True)
    dr.text((60, lg_y), "Notation:", font=f_lg, fill=INK)
    start_state(dr, 160, lg_y + 22)
    dr.text((180, lg_y + 15), "initial state", font=load_font(10.5), fill=INK)
    end_state(dr, 340, lg_y + 22, label="")
    dr.text((370, lg_y + 15), "final state", font=load_font(10.5), fill=INK)
    dr.line([(520, lg_y+22), (580, lg_y+22)], fill=ACCENT, width=2)
    arrowhead(dr, (580, lg_y+22), (1, 0), color=ACCENT)
    dr.text((590, lg_y + 15), "forward transition", font=load_font(10.5), fill=INK)
    dr.line([(760, lg_y+22), (820, lg_y+22)], fill=(150, 150, 160), width=2)
    arrowhead(dr, (820, lg_y+22), (1, 0), color=(150, 150, 160))
    dr.text((830, lg_y + 15), "exception / loop-back transition", font=load_font(10.5), fill=INK)

    save(img, os.path.join(OUT, "state_chart_diagram.png"))
    print("State chart saved.")


if __name__ == "__main__":
    main()
