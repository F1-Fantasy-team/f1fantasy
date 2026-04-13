package no.f1fantasy.service;

import no.f1fantasy.entity.*;
import no.f1fantasy.kafka.EventPublisher;
import no.f1fantasy.kafka.event.PredictionEvent;
import no.f1fantasy.repository.*;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.stereotype.Service;

import java.time.OffsetDateTime;
import java.util.List;
import java.util.NoSuchElementException;
import java.util.Objects;
import java.util.Optional;

@Service
public class PredictionService {

    private final GroupRepository groupRepository;
    private final GroupMemberRepository groupMemberRepository;
    private final ConstructorService constructorService;
    private final DriverService driverService;
    private final ConstructorChampionshipPredictionRepository constructorChampionshipPredictionRepository;
    private final DriverChampionshipPredictionRepository driverChampionshipPredictionRepository;
    private final DriverDraftPredictionRepository driverDraftPredictionRepository;
    private final DestructorPredictionRepository destructorPredictionRepository;
    private final MrSaturdayPredictionRepository mrSaturdayPredictionRepository;
    private final ZeroPointerPredictionRepository zeroPointerPredictionRepository;
    private final WildcardPredictionRepository wildcardPredictionRepository;
    private final EventPublisher eventPublisher;
    private final String predictionEventsTopic;

    public PredictionService(
        GroupRepository groupRepository,
        GroupMemberRepository groupMemberRepository,
        ConstructorService constructorService,
        DriverService driverService,
        ConstructorChampionshipPredictionRepository constructorChampionshipPredictionRepository,
        DriverChampionshipPredictionRepository driverChampionshipPredictionRepository,
        DriverDraftPredictionRepository driverDraftPredictionRepository,
        DestructorPredictionRepository destructorPredictionRepository,
        MrSaturdayPredictionRepository mrSaturdayPredictionRepository,
        ZeroPointerPredictionRepository zeroPointerPredictionRepository,
        WildcardPredictionRepository wildcardPredictionRepository,
        EventPublisher eventPublisher,
        @Value("${app.kafka.topics.prediction-events:f1.prediction.events}") String predictionEventsTopic
    ) {
        this.groupRepository = groupRepository;
        this.groupMemberRepository = groupMemberRepository;
        this.constructorService = constructorService;
        this.driverService = driverService;
        this.constructorChampionshipPredictionRepository = constructorChampionshipPredictionRepository;
        this.driverChampionshipPredictionRepository = driverChampionshipPredictionRepository;
        this.driverDraftPredictionRepository = driverDraftPredictionRepository;
        this.destructorPredictionRepository = destructorPredictionRepository;
        this.mrSaturdayPredictionRepository = mrSaturdayPredictionRepository;
        this.zeroPointerPredictionRepository = zeroPointerPredictionRepository;
        this.wildcardPredictionRepository = wildcardPredictionRepository;
        this.eventPublisher = eventPublisher;
        this.predictionEventsTopic = predictionEventsTopic;
    }

    public ConstructorChampionshipPrediction saveConstructorChampionship(Integer groupId, String userId, List<String> rankedConstructorIds) {
        validateGroupAndLock(groupId, userId);
        validateUniqueAndNotEmpty(rankedConstructorIds, "constructor IDs");

        String season = String.valueOf(OffsetDateTime.now().getYear());
        List<String> activeConstructorIds = constructorService.getActiveConstructors(season)
            .stream()
            .map(Constructor::getConstructorId)
            .toList();

        validateSameSet(rankedConstructorIds, activeConstructorIds, "constructors");

        ConstructorChampionshipPrediction prediction = constructorChampionshipPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(ConstructorChampionshipPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setRankedConstructorIds(rankedConstructorIds);
        setTimestamps(prediction);
        ConstructorChampionshipPrediction saved = constructorChampionshipPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "constructor_championship");
        return saved;
    }

    public Optional<ConstructorChampionshipPrediction> getConstructorChampionship(Integer groupId, String userId) {
        return constructorChampionshipPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public DriverChampionshipPrediction saveDriverChampionship(Integer groupId, String userId, List<String> rankedDriverIds) {
        validateGroupAndLock(groupId, userId);
        validateUniqueAndNotEmpty(rankedDriverIds, "driver IDs");

        String season = String.valueOf(OffsetDateTime.now().getYear());
        List<String> activeDriverIds = driverService.getActiveDrivers(season)
            .stream()
            .map(Driver::getDriverId)
            .toList();

        validateSameSet(rankedDriverIds, activeDriverIds, "drivers");

        DriverChampionshipPrediction prediction = driverChampionshipPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(DriverChampionshipPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setRankedDriverIds(rankedDriverIds);
        setTimestamps(prediction);
        DriverChampionshipPrediction saved = driverChampionshipPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "driver_championship");
        return saved;
    }

    public Optional<DriverChampionshipPrediction> getDriverChampionship(Integer groupId, String userId) {
        return driverChampionshipPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public DriverDraftPrediction saveDriverDraft(Integer groupId, String userId, String driver1Id, String driver2Id) {
        validateGroupAndLock(groupId, userId);
        validatePairOfDistinctActiveDrivers(driver1Id, driver2Id);

        DriverDraftPrediction prediction = driverDraftPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(DriverDraftPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setDriver1Id(driver1Id);
        prediction.setDriver2Id(driver2Id);
        setTimestamps(prediction);
        DriverDraftPrediction saved = driverDraftPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "driver_draft");
        return saved;
    }

    public Optional<DriverDraftPrediction> getDriverDraft(Integer groupId, String userId) {
        return driverDraftPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public DestructorPrediction saveDestructor(Integer groupId, String userId, String driver1Id, String driver2Id) {
        validateGroupAndLock(groupId, userId);
        validatePairOfDistinctActiveDrivers(driver1Id, driver2Id);

        DestructorPrediction prediction = destructorPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(DestructorPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setDriver1Id(driver1Id);
        prediction.setDriver2Id(driver2Id);
        setTimestamps(prediction);
        DestructorPrediction saved = destructorPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "destructor");
        return saved;
    }

    public Optional<DestructorPrediction> getDestructor(Integer groupId, String userId) {
        return destructorPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public MrSaturdayPrediction saveMrSaturday(Integer groupId, String userId, String driver1Id, String driver2Id) {
        validateGroupAndLock(groupId, userId);
        validatePairOfDistinctActiveDrivers(driver1Id, driver2Id);

        MrSaturdayPrediction prediction = mrSaturdayPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(MrSaturdayPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setDriver1Id(driver1Id);
        prediction.setDriver2Id(driver2Id);
        setTimestamps(prediction);
        MrSaturdayPrediction saved = mrSaturdayPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "mr_saturday");
        return saved;
    }

    public Optional<MrSaturdayPrediction> getMrSaturday(Integer groupId, String userId) {
        return mrSaturdayPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public ZeroPointerPrediction saveZeroPointer(Integer groupId, String userId, List<String> driverIds) {
        validateGroupAndLock(groupId, userId);
        validateUniqueAndNotEmpty(driverIds, "zero-pointer driver IDs");

        String season = String.valueOf(OffsetDateTime.now().getYear());
        List<String> activeDriverIds = driverService.getActiveDrivers(season)
            .stream()
            .map(Driver::getDriverId)
            .toList();

        boolean containsInvalid = driverIds.stream().anyMatch(id -> !activeDriverIds.contains(id));
        if (containsInvalid) {
            throw new IllegalArgumentException("Invalid driver ID provided for zero-pointer prediction");
        }

        ZeroPointerPrediction prediction = zeroPointerPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(ZeroPointerPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setDriverIds(driverIds);
        setTimestamps(prediction);
        ZeroPointerPrediction saved = zeroPointerPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "zero_pointer");
        return saved;
    }

    public Optional<ZeroPointerPrediction> getZeroPointer(Integer groupId, String userId) {
        return zeroPointerPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public WildcardPrediction saveWildcard(Integer groupId, String userId, String statement) {
        validateGroupAndLock(groupId, userId);
        if (statement != null && statement.length() > 500) {
            throw new IllegalArgumentException("Wildcard statement cannot exceed 500 characters");
        }

        WildcardPrediction prediction = wildcardPredictionRepository
            .findByGroupIdAndUserId(groupId, userId)
            .orElseGet(WildcardPrediction::new);

        prediction.setGroupId(groupId);
        prediction.setUserId(userId);
        prediction.setStatement(statement);
        setTimestamps(prediction);
        WildcardPrediction saved = wildcardPredictionRepository.save(prediction);
        publishPredictionSaved(groupId, userId, "wildcard");
        return saved;
    }

    public Optional<WildcardPrediction> getWildcard(Integer groupId, String userId) {
        return wildcardPredictionRepository.findByGroupIdAndUserId(groupId, userId);
    }

    public List<WildcardPrediction> getAllWildcards(Integer groupId, String userId) {
        if (!groupMemberRepository.existsByGroupIdAndUserId(groupId, userId)) {
            throw new SecurityException("User is not a member of this group");
        }
        return wildcardPredictionRepository.findByGroupId(groupId);
    }

    private void validateGroupAndLock(Integer groupId, String userId) {
        Integer safeGroupId = Objects.requireNonNull(groupId, "groupId must not be null");
        String safeUserId = Objects.requireNonNull(userId, "userId must not be null");

        Group group = groupRepository.findById(safeGroupId)
            .orElseThrow(() -> new NoSuchElementException("Group not found"));

        if (!groupMemberRepository.existsByGroupIdAndUserId(safeGroupId, safeUserId)) {
            throw new SecurityException("User is not a member of this group");
        }

        if (group.isPredictionsLocked()) {
            throw new IllegalStateException("Predictions are locked for this group");
        }
    }

    private void validateUniqueAndNotEmpty(List<String> ids, String label) {
        List<String> safeIds = Objects.requireNonNull(ids, label + " must not be null");
        if (safeIds.isEmpty()) {
            throw new IllegalArgumentException(label + " must not be empty");
        }
        if (safeIds.size() != safeIds.stream().distinct().count()) {
            throw new IllegalArgumentException(label + " must be unique");
        }
    }

    private void validateSameSet(List<String> provided, List<String> expected, String label) {
        if (provided.size() != expected.size()) {
            throw new IllegalArgumentException("Must provide all active " + label);
        }

        boolean hasInvalid = provided.stream().anyMatch(id -> !expected.contains(id));
        if (hasInvalid) {
            throw new IllegalArgumentException("Invalid " + label + " IDs provided");
        }
    }

    private void validatePairOfDistinctActiveDrivers(String driver1Id, String driver2Id) {
        if (driver1Id != null && driver2Id != null && driver1Id.equals(driver2Id)) {
            throw new IllegalArgumentException("Cannot select the same driver twice");
        }

        String season = String.valueOf(OffsetDateTime.now().getYear());
        List<String> activeDriverIds = driverService.getActiveDrivers(season)
            .stream()
            .map(Driver::getDriverId)
            .toList();

        if (driver1Id != null && !activeDriverIds.contains(driver1Id)) {
            throw new IllegalArgumentException("Invalid driver1 ID");
        }

        if (driver2Id != null && !activeDriverIds.contains(driver2Id)) {
            throw new IllegalArgumentException("Invalid driver2 ID");
        }
    }

    private void setTimestamps(Object prediction) {
        OffsetDateTime now = OffsetDateTime.now();

        switch (prediction) {
            case ConstructorChampionshipPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case DriverChampionshipPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case DriverDraftPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case DestructorPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case MrSaturdayPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case ZeroPointerPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            case WildcardPrediction entity -> applyTimestamps(entity.getCreatedAt(), entity::setCreatedAt, entity::setUpdatedAt, now);
            default -> {
            }
        }
    }

    private void applyTimestamps(
        OffsetDateTime createdAt,
        java.util.function.Consumer<OffsetDateTime> setCreatedAt,
        java.util.function.Consumer<OffsetDateTime> setUpdatedAt,
        OffsetDateTime now
    ) {
        if (createdAt == null) {
            setCreatedAt.accept(now);
        }
        setUpdatedAt.accept(now);
    }

    private void publishPredictionSaved(Integer groupId, String userId, String predictionType) {
        eventPublisher.publish(
            predictionEventsTopic,
            groupId + ":" + userId,
            new PredictionEvent(
                "PREDICTION_SAVED",
                predictionType,
                groupId,
                userId,
                OffsetDateTime.now()
            )
        );
    }
}
