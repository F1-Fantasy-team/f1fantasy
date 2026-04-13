package no.f1fantasy.service;

import no.f1fantasy.entity.*;
import no.f1fantasy.kafka.EventPublisher;
import no.f1fantasy.repository.*;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.extension.ExtendWith;
import org.mockito.Mock;
import org.mockito.junit.jupiter.MockitoExtension;

import java.util.List;
import java.util.Optional;

import static org.assertj.core.api.Assertions.assertThat;
import static org.assertj.core.api.Assertions.assertThatThrownBy;
import static org.mockito.ArgumentMatchers.any;
import static org.mockito.Mockito.*;

@ExtendWith(MockitoExtension.class)
@SuppressWarnings("null")
class PredictionServiceTest {

    @Mock
    private GroupRepository groupRepository;

    @Mock
    private GroupMemberRepository groupMemberRepository;

    @Mock
    private ConstructorService constructorService;

    @Mock
    private DriverService driverService;

    @Mock
    private ConstructorChampionshipPredictionRepository constructorChampionshipPredictionRepository;

    @Mock
    private DriverChampionshipPredictionRepository driverChampionshipPredictionRepository;

    @Mock
    private DriverDraftPredictionRepository driverDraftPredictionRepository;

    @Mock
    private DestructorPredictionRepository destructorPredictionRepository;

    @Mock
    private MrSaturdayPredictionRepository mrSaturdayPredictionRepository;

    @Mock
    private ZeroPointerPredictionRepository zeroPointerPredictionRepository;

    @Mock
    private WildcardPredictionRepository wildcardPredictionRepository;

    @Mock
    private EventPublisher eventPublisher;

    @Test
    void shouldSaveConstructorChampionshipPredictionWhenValid() {
        Group group = unlockedGroup(1, "admin-1");

        Constructor c1 = new Constructor();
        c1.setConstructorId("mclaren");
        Constructor c2 = new Constructor();
        c2.setConstructorId("ferrari");

        when(groupRepository.findById(1)).thenReturn(Optional.of(group));
        when(groupMemberRepository.existsByGroupIdAndUserId(1, "user-1")).thenReturn(true);
        when(constructorService.getActiveConstructors(any())).thenReturn(List.of(c1, c2));
        when(constructorChampionshipPredictionRepository.findByGroupIdAndUserId(1, "user-1")).thenReturn(Optional.empty());
        when(constructorChampionshipPredictionRepository.save(any(ConstructorChampionshipPrediction.class)))
            .thenAnswer(invocation -> invocation.getArgument(0));

        PredictionService service = createService();
        ConstructorChampionshipPrediction prediction = service.saveConstructorChampionship(
            1,
            "user-1",
            List.of("mclaren", "ferrari")
        );

        assertThat(prediction.getRankedConstructorIds()).containsExactly("mclaren", "ferrari");
        assertThat(prediction.getCreatedAt()).isNotNull();
        verify(constructorChampionshipPredictionRepository, times(1)).save(any(ConstructorChampionshipPrediction.class));
        verify(eventPublisher, times(1)).publish(eq("f1.prediction.events"), eq("1:user-1"), any());
    }

    @Test
    void shouldRejectWhenGroupPredictionsLocked() {
        Group group = unlockedGroup(2, "admin-2");
        group.setPredictionsLocked(true);

        when(groupRepository.findById(2)).thenReturn(Optional.of(group));
        when(groupMemberRepository.existsByGroupIdAndUserId(2, "user-2")).thenReturn(true);

        PredictionService service = createService();

        assertThatThrownBy(() -> service.saveWildcard(2, "user-2", "bold take"))
            .isInstanceOf(IllegalStateException.class)
            .hasMessageContaining("locked");
    }

    @Test
    void shouldRejectDriverDraftWithDuplicateDriver() {
        Group group = unlockedGroup(3, "admin-3");

        when(groupRepository.findById(3)).thenReturn(Optional.of(group));
        when(groupMemberRepository.existsByGroupIdAndUserId(3, "user-3")).thenReturn(true);

        PredictionService service = createService();

        assertThatThrownBy(() -> service.saveDriverDraft(3, "user-3", "norris", "norris"))
            .isInstanceOf(IllegalArgumentException.class)
            .hasMessageContaining("same driver twice");
    }

    @Test
    void shouldSaveWildcardAndFetchAllWildcardsForMember() {
        Group group = unlockedGroup(4, "admin-4");
        WildcardPrediction stored = new WildcardPrediction();
        stored.setGroupId(4);
        stored.setUserId("user-4");
        stored.setStatement("A safety car in every race");

        when(groupRepository.findById(4)).thenReturn(Optional.of(group));
        when(groupMemberRepository.existsByGroupIdAndUserId(4, "user-4")).thenReturn(true);
        when(wildcardPredictionRepository.findByGroupIdAndUserId(4, "user-4")).thenReturn(Optional.empty());
        when(wildcardPredictionRepository.save(any(WildcardPrediction.class))).thenReturn(stored);
        when(wildcardPredictionRepository.findByGroupId(4)).thenReturn(List.of(stored));

        PredictionService service = createService();
        WildcardPrediction saved = service.saveWildcard(4, "user-4", "A safety car in every race");
        List<WildcardPrediction> all = service.getAllWildcards(4, "user-4");

        assertThat(saved.getStatement()).isEqualTo("A safety car in every race");
        assertThat(all).hasSize(1);
    }

    private PredictionService createService() {
        return new PredictionService(
            groupRepository,
            groupMemberRepository,
            constructorService,
            driverService,
            constructorChampionshipPredictionRepository,
            driverChampionshipPredictionRepository,
            driverDraftPredictionRepository,
            destructorPredictionRepository,
            mrSaturdayPredictionRepository,
            zeroPointerPredictionRepository,
            wildcardPredictionRepository,
            eventPublisher,
            "f1.prediction.events"
        );
    }

    private Group unlockedGroup(int id, String adminUserId) {
        Group group = new Group();
        group.setId(id);
        group.setAdminUserId(adminUserId);
        group.setPredictionsLocked(false);
        return group;
    }
}
