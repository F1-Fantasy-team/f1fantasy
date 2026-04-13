package no.f1fantasy.repository;

import no.f1fantasy.entity.Status;
import org.springframework.data.jpa.repository.JpaRepository;

import java.util.Optional;

public interface StatusRepository extends JpaRepository<Status, String> {
	Optional<Status> findByStatusText(String statusText);
}
