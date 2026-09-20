from cfip.domain.artifact_attestation import compute_sha256, verify_sha256


def test_sha256_digest_is_real_and_deterministic() -> None:
    artifact = b"cfip-artifact"
    digest = compute_sha256(artifact)
    assert len(digest) == 64
    assert verify_sha256(artifact, digest)
    assert not verify_sha256(b"tampered", digest)
    assert not verify_sha256(artifact, "0" * 63)
