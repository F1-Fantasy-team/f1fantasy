package no.f1fantasy.repository;

import no.f1fantasy.entity.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.orm.jpa.DataJpaTest;
import org.springframework.context.annotation.Import;
import org.springframework.test.context.ActiveProfiles;

import java.time.OffsetDateTime;
import java.util.List;

import static org.assertj.core.api.Assertions.assertThat;

/**
 * Repository integration tests using H2 in-memory database.
 * Schema is created by Hibernate (ddl-auto=create-drop in test profile).
 * Every test cleans up its own data in {@link #cleanup()}.
 */
@DataJpaTest
@ActiveProfiles("test")
@Import(no.f1fantasy.config.EfCoreNamingStrategy.class)
class RepositoryIntegrationTest {

    @Autowired SeasonRepository seasonRepository;
    @Autowired CircuitRepository circuitRepository;
    @Autowired DriverRepository driverRepository;
    @Autowired ConstructorRepository constructorRepository;
    @Autowired RaceRepository raceRepository;
    @Autowired ResultRepository resultRepository;
    @Autowired QualifyingRepository qualifyingRepository;
    @Autowired PitStopRepository pitStopRepository;
    @Autowired LapTimingRepository lapTimingRepository;
    @Autowired DriverStandingRepository driverStandingRepository;
    @Autowired ConstructorStandingRepository constructorStandingRepository;
    @Autowired StatusRepository statusRepository;
    @Autowired DataFetchMetadataRepository dataFetchMetadataRepository;
    @Autowired GroupRepository groupRepository;
    @Autowired GroupMemberRepository groupMemberRepository;
    @Autowired DriverChampionshipPredictionRepository driverChampionshipPredictionRepository;
    @Autowired WildcardPredictionRepository wildcardPredictionRepository;
    @Autowired StandingRepository standingRepository;

    @SuppressWarnings("unused")
    @AfterEach
    void tearDown() {
        standingRepository.deleteAll();
        wildcardPredictionRepository.deleteAll();
        driverChampionshipPredictionRepository.deleteAll();
        groupMemberRepository.deleteAll();
        groupRepository.deleteAll();
        dataFetchMetadataRepository.deleteAll();
        lapTimingRepository.deleteAll();
        pitStopRepository.deleteAll();
        qualifyingRepository.deleteAll();
        resultRepository.deleteAll();
        raceRepository.deleteAll();
        constructorStandingRepository.deleteAll();
        driverStandingRepository.deleteAll();
        statusRepository.deleteAll();
        constructorRepository.deleteAll();
        driverRepository.deleteAll();
        circuitRepository.deleteAll();
        seasonRepository.deleteAll();
    }

    // ── Season ─────────────────────────────────────────────────────────

    @Test
    @DisplayName("Season: save and find by year")
    void season_saveAndFind() {
        Season s = new Season();
        s.setYear("2026");
        s.setUrl("https://en.wikipedia.org/wiki/2026_Formula_One_World_Championship");
        seasonRepository.save(s);

        assertThat(seasonRepository.findByYear("2026")).isPresent();
        assertThat(seasonRepository.findAll()).hasSize(1);
    }

    // ── Circuit ────────────────────────────────────────────────────────

    @Test
    @DisplayName("Circuit: save with embedded Location")
    void circuit_saveWithLocation() {
        Circuit c = new Circuit();
        c.setCircuitId("monaco");
        c.setCircuitName("Circuit de Monaco");
        c.setUrl("https://en.wikipedia.org/wiki/Circuit_de_Monaco");
        Location loc = new Location();
        loc.setLat("43.7347");
        loc.setLong_("7.42056");
        loc.setLocality("Monte-Carlo");
        loc.setCountry("Monaco");
        c.setLocation(loc);
        circuitRepository.save(c);

        Circuit found = circuitRepository.findByCircuitId("monaco").orElseThrow();
        assertThat(found.getLocation().getLocality()).isEqualTo("Monte-Carlo");
    }

    // ── Driver ────────────────────────────────────────────────────────

    @Test
    @DisplayName("Driver: save with activeSeasons list")
    void driver_saveWithActiveSeasons() {
        Driver d = new Driver();
        d.setDriverId("hamilton");
        d.setGivenName("Lewis");
        d.setFamilyName("Hamilton");
        d.setCode("HAM");
        d.setPermanentNumber("44");
        d.setNationality("British");
        d.setDateOfBirth("1985-01-07");
        d.setUrl("https://en.wikipedia.org/wiki/Lewis_Hamilton");
        d.setActiveSeasons(List.of("2024", "2025", "2026"));
        driverRepository.save(d);

        Driver found = driverRepository.findByDriverId("hamilton").orElseThrow();
        assertThat(found.getActiveSeasons()).containsExactlyInAnyOrder("2024", "2025", "2026");
    }

    // ── Constructor ───────────────────────────────────────────────────

    @Test
    @DisplayName("Constructor: save and find by id")
    void constructor_saveAndFind() {
        Constructor c = new Constructor();
        c.setConstructorId("ferrari");
        c.setName("Ferrari");
        c.setNationality("Italian");
        c.setUrl("https://en.wikipedia.org/wiki/Scuderia_Ferrari");
        c.setActiveSeasons(List.of("2025", "2026"));
        constructorRepository.save(c);

        assertThat(constructorRepository.findByConstructorId("ferrari")).isPresent();
    }

    // ── Race ──────────────────────────────────────────────────────────

    @Test
    @DisplayName("Race: save with composite PK and sessions")
    void race_saveWithCompositePk() {
        Season season = new Season();
        season.setYear("2026");
        season.setUrl("http://example.com");
        seasonRepository.save(season);

        Race r = new Race();
        r.setSeason("2026");
        r.setRound("1");
        r.setRaceName("Australian Grand Prix");
        r.setUrl("http://example.com/race");
        r.setDate("2026-03-15");
        r.setTime("05:00:00Z");
        Session q = new Session();
        q.setDate("2026-03-14");
        q.setTime("06:00:00Z");
        r.setQualifying(q);
        raceRepository.save(r);

        Race found = raceRepository.findBySeasonAndRound("2026", "1").orElseThrow();
        assertThat(found.getRaceName()).isEqualTo("Australian Grand Prix");
        assertThat(found.getQualifying().getDate()).isEqualTo("2026-03-14");
    }

    @Test
    @DisplayName("Race: findBySeasonOrderByRoundAsc returns rounds in order")
    void race_findBySeason_orderedByRound() {
        Season season = new Season();
        season.setYear("2026");
        season.setUrl("http://example.com");
        seasonRepository.save(season);

        for (int i = 3; i >= 1; i--) {
            Race r = new Race();
            r.setSeason("2026");
            r.setRound(String.valueOf(i));
            r.setRaceName("Race " + i);
            r.setUrl("http://example.com");
            r.setDate("2026-0" + i + "-01");
            r.setTime("12:00:00Z");
            raceRepository.save(r);
        }

        List<Race> ordered = raceRepository.findBySeasonOrderByRoundAsc("2026");
        assertThat(ordered).extracting(Race::getRound).containsExactly("1", "2", "3");
    }

    // ── Result ────────────────────────────────────────────────────────

    @Test
    @DisplayName("Result: save and find by season+round")
    void result_saveAndFind() {
        Result res = new Result();
        res.setSeason("2026");
        res.setRound("1");
        res.setDriverId("hamilton");
        res.setConstructorId("ferrari");
        res.setPosition("1");
        res.setPositionText("1");
        res.setPoints("25");
        res.setNumber("44");
        res.setLaps("58");
        res.setStatus("Finished");
        res.setSprint(false);
        resultRepository.save(res);

        List<Result> results = resultRepository.findBySeasonAndRound("2026", "1");
        assertThat(results).hasSize(1);
        assertThat(results.get(0).getDriverId()).isEqualTo("hamilton");
    }

    // ── DataFetchMetadata ────────────────────────────────────────────

    @Test
    @DisplayName("DataFetchMetadata: save and find by season+dataType")
    void dataFetchMetadata_saveAndFind() {
        DataFetchMetadata meta = new DataFetchMetadata();
        meta.setSeason("2026");
        meta.setDataType("Races");
        meta.setLastFetchedAt(OffsetDateTime.now());
        meta.setFetchSuccessful(true);
        dataFetchMetadataRepository.save(meta);

        assertThat(dataFetchMetadataRepository.findBySeasonAndDataType("2026", "Races")).isPresent();
        assertThat(dataFetchMetadataRepository.existsBySeasonAndDataType("2026", "Races")).isTrue();
        assertThat(dataFetchMetadataRepository.existsBySeasonAndDataType("2026", "Results")).isFalse();
    }

    // ── Group + Member ────────────────────────────────────────────────

    @Test
    @DisplayName("Group: create group and add member")
    void group_createAndAddMember() {
        Group g = new Group();
        g.setName("Test Group");
        g.setInviteCode("ABC123");
        g.setLockMode("system");
        g.setAdminUserId("user_abc");
        g.setCreatedAt(OffsetDateTime.now());
        g.setPredictionsLocked(false);
        Group saved = groupRepository.save(g);

        GroupMember m = new GroupMember();
        m.setGroupId(saved.getId());
        m.setUserId("user_abc");
        m.setJoinedAt(OffsetDateTime.now());
        groupMemberRepository.save(m);

        assertThat(groupMemberRepository.existsByGroupIdAndUserId(saved.getId(), "user_abc")).isTrue();
        assertThat(groupRepository.findByMembers_UserId("user_abc")).hasSize(1);
    }

    @Test
    @DisplayName("Group: findByLockModeInAndPredictionsLockedFalse returns unlocked groups")
    void group_findUnlocked() {
        Group g1 = new Group();
        g1.setName("Unlocked System");
        g1.setInviteCode("CODE1");
        g1.setLockMode("system");
        g1.setAdminUserId("user1");
        g1.setCreatedAt(OffsetDateTime.now());
        g1.setPredictionsLocked(false);
        groupRepository.save(g1);

        Group g2 = new Group();
        g2.setName("Already Locked");
        g2.setInviteCode("CODE2");
        g2.setLockMode("system");
        g2.setAdminUserId("user2");
        g2.setCreatedAt(OffsetDateTime.now());
        g2.setPredictionsLocked(true);
        groupRepository.save(g2);

        List<Group> unlocked = groupRepository.findByLockModeInAndPredictionsLockedFalse(
                List.of("system", "hybrid"));
        assertThat(unlocked).hasSize(1);
        assertThat(unlocked.get(0).getInviteCode()).isEqualTo("CODE1");
    }

    // ── DriverChampionshipPrediction ──────────────────────────────────

    @Test
    @DisplayName("DriverChampionshipPrediction: save and retrieve JSON list")
    void driverChampionshipPrediction_jsonRoundTrip() {
        Group g = new Group();
        g.setName("Pred Group");
        g.setInviteCode("PRED1");
        g.setLockMode("admin");
        g.setAdminUserId("adminUser");
        g.setCreatedAt(OffsetDateTime.now());
        g.setPredictionsLocked(false);
        Group saved = groupRepository.save(g);

        DriverChampionshipPrediction pred = new DriverChampionshipPrediction();
        pred.setGroupId(saved.getId());
        pred.setUserId("user_x");
        pred.setRankedDriverIds(List.of("verstappen", "hamilton", "leclerc"));
        pred.setCreatedAt(OffsetDateTime.now());
        driverChampionshipPredictionRepository.save(pred);

        DriverChampionshipPrediction found = driverChampionshipPredictionRepository
                .findByGroupIdAndUserId(saved.getId(), "user_x")
                .orElseThrow();
        assertThat(found.getRankedDriverIds())
                .containsExactly("verstappen", "hamilton", "leclerc");
    }

    // ── Standing ──────────────────────────────────────────────────────

    @Test
    @DisplayName("Standing: save and find ordered by rank")
    void standing_saveAndFindOrdered() {
        Group g = new Group();
        g.setName("Standings Group");
        g.setInviteCode("STD1");
        g.setLockMode("admin");
        g.setAdminUserId("adminUser");
        g.setCreatedAt(OffsetDateTime.now());
        g.setPredictionsLocked(false);
        Group savedGroup = groupRepository.save(g);

        for (int rank = 3; rank >= 1; rank--) {
            Standing st = new Standing();
            st.setGroupId(savedGroup.getId());
            st.setUserId("user_" + rank);
            st.setTotalScore(rank * 10);
            st.setRank(rank);
            st.setUpdatedAt(OffsetDateTime.now());
            standingRepository.save(st);
        }

        List<Standing> standings = standingRepository.findByGroupIdOrderByRankAsc(savedGroup.getId());
        assertThat(standings).extracting(Standing::getRank).containsExactly(1, 2, 3);
    }
}
