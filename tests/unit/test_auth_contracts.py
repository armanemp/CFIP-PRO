from cfip.domain.auth_contracts import Principal, authorize

def test_user_has_read_only_terminal_and_git_visibility():
    principal = Principal(subject="u1")
    assert authorize(principal, "terminal:read").allowed
    assert not authorize(principal, "git:apply").allowed

def test_admin_can_propose_and_apply_governed_changes():
    principal = Principal(subject="a1", roles=("admin",))
    assert authorize(principal, "git:propose").allowed
    assert authorize(principal, "git:apply").allowed
