package no.f1fantasy.controller;

import java.security.Principal;
import java.util.List;

import org.springframework.http.HttpStatus;
import org.springframework.http.ResponseEntity;
import org.springframework.security.access.prepost.PreAuthorize;
import org.springframework.web.bind.annotation.DeleteMapping;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.PutMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import no.f1fantasy.dto.CreateGroupRequest;
import no.f1fantasy.dto.ErrorResponse;
import no.f1fantasy.dto.RenameGroupRequest;
import no.f1fantasy.dto.SuccessResponse;
import no.f1fantasy.entity.Group;
import no.f1fantasy.entity.GroupMember;
import no.f1fantasy.service.GroupService;

@RestController
@RequestMapping("/api/groups")
@PreAuthorize("isAuthenticated()")
@SuppressWarnings("null")
public class GroupsController {
    
    private final GroupService groupService;

    public GroupsController(GroupService groupService) {
        this.groupService = groupService;
    }

    private String getUserId(Principal principal) {
        if (principal == null) {
            throw new IllegalArgumentException("User ID not found");
        }
        return principal.getName();
    }

    @PostMapping
    public ResponseEntity<?> createGroup(@RequestBody CreateGroupRequest request, Principal principal) {
        try {
            String userId = getUserId(principal);
            Group group = groupService.createGroup(request.getName(), userId, request.getLockMode());
            return ResponseEntity.ok(group);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping
    public ResponseEntity<?> getMyGroups(Principal principal) {
        try {
            String userId = getUserId(principal);
            List<Group> groups = groupService.getUserGroups(userId);
            return ResponseEntity.ok(groups);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/{id}")
    public ResponseEntity<?> getGroup(@PathVariable int id) {
        try {
            var group = groupService.getGroupById(id);
            if (group.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(group.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @GetMapping("/invite/{inviteCode}")
    public ResponseEntity<?> getGroupByInviteCode(@PathVariable String inviteCode) {
        try {
            var group = groupService.getGroupByInviteCode(inviteCode);
            if (group.isEmpty()) {
                return ResponseEntity.notFound().build();
            }
            return ResponseEntity.ok(group.get());
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PostMapping("/{id}/join")
    public ResponseEntity<?> joinGroup(@PathVariable int id, Principal principal) {
        try {
            String userId = getUserId(principal);
            GroupMember member = groupService.joinGroup(id, userId);
            return ResponseEntity.ok(member);
        } catch (IllegalStateException ex) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PostMapping("/{id}/leave")
    public ResponseEntity<?> leaveGroup(@PathVariable int id, Principal principal) {
        try {
            String userId = getUserId(principal);
            groupService.leaveGroup(id, userId);
            return ResponseEntity.ok(new SuccessResponse("Left group successfully"));
        } catch (IllegalStateException ex) {
            return ResponseEntity.status(HttpStatus.CONFLICT).body(new ErrorResponse(ex.getMessage()));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PutMapping("/{id}")
    public ResponseEntity<?> renameGroup(@PathVariable int id, @RequestBody RenameGroupRequest request, Principal principal) {
        try {
            String userId = getUserId(principal);
            Group group = groupService.renameGroup(id, userId, request.getNewName());
            return ResponseEntity.ok(group);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @DeleteMapping("/{id}")
    public ResponseEntity<?> deleteGroup(@PathVariable int id, Principal principal) {
        try {
            String userId = getUserId(principal);
            groupService.deleteGroup(id, userId);
            return ResponseEntity.ok(new SuccessResponse("Group deleted successfully"));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PostMapping("/{id}/lock")
    public ResponseEntity<?> lockPredictions(@PathVariable int id, Principal principal) {
        try {
            String userId = getUserId(principal);
            Group group = groupService.lockPredictions(id, userId);
            return ResponseEntity.ok(group);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @PostMapping("/{id}/unlock")
    public ResponseEntity<?> unlockPredictions(@PathVariable int id, Principal principal) {
        try {
            String userId = getUserId(principal);
            Group group = groupService.unlockPredictions(id, userId);
            return ResponseEntity.ok(group);
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }

    @DeleteMapping("/{groupId}/members/{userId}")
    public ResponseEntity<?> removeMember(
            @PathVariable int groupId,
            @PathVariable String userId,
            Principal principal) {
        try {
            String adminUserId = getUserId(principal);
            groupService.removeMember(groupId, adminUserId, userId);
            return ResponseEntity.ok(new SuccessResponse("Member removed successfully"));
        } catch (Exception ex) {
            return ResponseEntity.badRequest().body(new ErrorResponse(ex.getMessage()));
        }
    }
}
