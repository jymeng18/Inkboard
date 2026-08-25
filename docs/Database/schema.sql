--
-- PostgreSQL database dump
--

-- Dumped from database version 16.15 (Ubuntu 16.15-0ubuntu0.24.04.1)
-- Dumped by pg_dump version 16.15 (Ubuntu 16.15-0ubuntu0.24.04.1)

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Block_List; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Block_List" (
    "UserId" uuid NOT NULL,
    "BlockedUserId" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: Canvas_Operations; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Canvas_Operations" (
    "Id" uuid NOT NULL,
    "Type" integer NOT NULL,
    "OperationData" jsonb NOT NULL,
    "Timestamp" timestamp with time zone NOT NULL,
    "CanvasId" uuid NOT NULL,
    "UserId" uuid
);


--
-- Name: Canvases; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Canvases" (
    "Id" uuid NOT NULL,
    "OwnerId" uuid NOT NULL,
    "Name" character varying(50),
    "SnapshotURL" text,
    "SnapshotTakenAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "LastModifiedAt" timestamp with time zone NOT NULL
);


--
-- Name: Friend_Requests; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Friend_Requests" (
    "Id" uuid NOT NULL,
    "RequesterId" uuid NOT NULL,
    "RequesteeId" uuid NOT NULL,
    "Status" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: Friendships; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Friendships" (
    "UserId1" uuid NOT NULL,
    "UserId2" uuid NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    CONSTRAINT "CK_Friendships_UserOrder" CHECK (("UserId1" < "UserId2"))
);


--
-- Name: Parties; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Parties" (
    "Id" uuid NOT NULL,
    "LeaderId" uuid NOT NULL,
    "CanvasId" uuid,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: Party_Invites; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Party_Invites" (
    "Id" uuid NOT NULL,
    "PartyId" uuid NOT NULL,
    "InvitedByUserId" uuid NOT NULL,
    "InvitedUserId" uuid NOT NULL,
    "InviteStatus" integer NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone NOT NULL
);


--
-- Name: Party_Members; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Party_Members" (
    "PartyId" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "Role" integer NOT NULL,
    "JoinedAt" timestamp with time zone NOT NULL
);


--
-- Name: RefreshTokens; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."RefreshTokens" (
    "Id" uuid NOT NULL,
    "TokenHash" text,
    "ExpiresAt" timestamp with time zone NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "IsRevoked" boolean NOT NULL,
    "UserId" uuid NOT NULL
);


--
-- Name: Users; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."Users" (
    "Id" uuid NOT NULL,
    "UserName" text,
    "Email" text,
    "PasswordHash" text,
    "CreatedAt" timestamp with time zone NOT NULL
);


--
-- Name: __EFMigrationsHistory; Type: TABLE; Schema: public; Owner: -
--

CREATE TABLE public."__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL
);


--
-- Name: Block_List PK_Block_List; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Block_List"
    ADD CONSTRAINT "PK_Block_List" PRIMARY KEY ("BlockedUserId", "UserId");


--
-- Name: Canvas_Operations PK_Canvas_Operations; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Canvas_Operations"
    ADD CONSTRAINT "PK_Canvas_Operations" PRIMARY KEY ("Id");


--
-- Name: Canvases PK_Canvases; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Canvases"
    ADD CONSTRAINT "PK_Canvases" PRIMARY KEY ("Id");


--
-- Name: Friend_Requests PK_Friend_Requests; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friend_Requests"
    ADD CONSTRAINT "PK_Friend_Requests" PRIMARY KEY ("Id");


--
-- Name: Friendships PK_Friendships; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT "PK_Friendships" PRIMARY KEY ("UserId1", "UserId2");


--
-- Name: Parties PK_Parties; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Parties"
    ADD CONSTRAINT "PK_Parties" PRIMARY KEY ("Id");


--
-- Name: Party_Invites PK_Party_Invites; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Invites"
    ADD CONSTRAINT "PK_Party_Invites" PRIMARY KEY ("Id");


--
-- Name: Party_Members PK_Party_Members; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Members"
    ADD CONSTRAINT "PK_Party_Members" PRIMARY KEY ("PartyId", "UserId");


--
-- Name: RefreshTokens PK_RefreshTokens; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("Id");


--
-- Name: Users PK_Users; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT "PK_Users" PRIMARY KEY ("Id");


--
-- Name: __EFMigrationsHistory PK___EFMigrationsHistory; Type: CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."__EFMigrationsHistory"
    ADD CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId");


--
-- Name: IX_Block_List_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Block_List_UserId" ON public."Block_List" USING btree ("UserId");


--
-- Name: IX_Canvas_Operations_CanvasId_Timestamp; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Canvas_Operations_CanvasId_Timestamp" ON public."Canvas_Operations" USING btree ("CanvasId", "Timestamp");


--
-- Name: IX_Canvas_Operations_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Canvas_Operations_UserId" ON public."Canvas_Operations" USING btree ("UserId");


--
-- Name: IX_Canvases_OwnerId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Canvases_OwnerId" ON public."Canvases" USING btree ("OwnerId");


--
-- Name: IX_Friend_Requests_RequesteeId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Friend_Requests_RequesteeId" ON public."Friend_Requests" USING btree ("RequesteeId");


--
-- Name: IX_Friend_Requests_RequesterId_RequesteeId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Friend_Requests_RequesterId_RequesteeId" ON public."Friend_Requests" USING btree ("RequesterId", "RequesteeId") WHERE ("Status" = 0);


--
-- Name: IX_Friendships_UserId2; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Friendships_UserId2" ON public."Friendships" USING btree ("UserId2");


--
-- Name: IX_Parties_CanvasId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Parties_CanvasId" ON public."Parties" USING btree ("CanvasId");


--
-- Name: IX_Parties_LeaderId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Parties_LeaderId" ON public."Parties" USING btree ("LeaderId");


--
-- Name: IX_Party_Invites_InvitedByUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Party_Invites_InvitedByUserId" ON public."Party_Invites" USING btree ("InvitedByUserId");


--
-- Name: IX_Party_Invites_InvitedUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Party_Invites_InvitedUserId" ON public."Party_Invites" USING btree ("InvitedUserId");


--
-- Name: IX_Party_Invites_PartyId_InvitedUserId; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Party_Invites_PartyId_InvitedUserId" ON public."Party_Invites" USING btree ("PartyId", "InvitedUserId") WHERE ("InviteStatus" = 0);


--
-- Name: IX_Party_Members_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_Party_Members_UserId" ON public."Party_Members" USING btree ("UserId");


--
-- Name: IX_RefreshTokens_TokenHash; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_RefreshTokens_TokenHash" ON public."RefreshTokens" USING btree ("TokenHash");


--
-- Name: IX_RefreshTokens_UserId; Type: INDEX; Schema: public; Owner: -
--

CREATE INDEX "IX_RefreshTokens_UserId" ON public."RefreshTokens" USING btree ("UserId");


--
-- Name: IX_Users_Email; Type: INDEX; Schema: public; Owner: -
--

CREATE UNIQUE INDEX "IX_Users_Email" ON public."Users" USING btree ("Email");


--
-- Name: Block_List FK_Block_List_Users_BlockedUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Block_List"
    ADD CONSTRAINT "FK_Block_List_Users_BlockedUserId" FOREIGN KEY ("BlockedUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Block_List FK_Block_List_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Block_List"
    ADD CONSTRAINT "FK_Block_List_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Canvas_Operations FK_Canvas_Operations_Canvases_CanvasId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Canvas_Operations"
    ADD CONSTRAINT "FK_Canvas_Operations_Canvases_CanvasId" FOREIGN KEY ("CanvasId") REFERENCES public."Canvases"("Id") ON DELETE CASCADE;


--
-- Name: Canvas_Operations FK_Canvas_Operations_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Canvas_Operations"
    ADD CONSTRAINT "FK_Canvas_Operations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE SET NULL;


--
-- Name: Canvases FK_Canvases_Users_OwnerId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Canvases"
    ADD CONSTRAINT "FK_Canvases_Users_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Friend_Requests FK_Friend_Requests_Users_RequesteeId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friend_Requests"
    ADD CONSTRAINT "FK_Friend_Requests_Users_RequesteeId" FOREIGN KEY ("RequesteeId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Friend_Requests FK_Friend_Requests_Users_RequesterId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friend_Requests"
    ADD CONSTRAINT "FK_Friend_Requests_Users_RequesterId" FOREIGN KEY ("RequesterId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Friendships FK_Friendships_Users_UserId1; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT "FK_Friendships_Users_UserId1" FOREIGN KEY ("UserId1") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Friendships FK_Friendships_Users_UserId2; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT "FK_Friendships_Users_UserId2" FOREIGN KEY ("UserId2") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Parties FK_Parties_Canvases_CanvasId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Parties"
    ADD CONSTRAINT "FK_Parties_Canvases_CanvasId" FOREIGN KEY ("CanvasId") REFERENCES public."Canvases"("Id") ON DELETE SET NULL;


--
-- Name: Parties FK_Parties_Users_LeaderId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Parties"
    ADD CONSTRAINT "FK_Parties_Users_LeaderId" FOREIGN KEY ("LeaderId") REFERENCES public."Users"("Id") ON DELETE RESTRICT;


--
-- Name: Party_Invites FK_Party_Invites_Parties_PartyId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Invites"
    ADD CONSTRAINT "FK_Party_Invites_Parties_PartyId" FOREIGN KEY ("PartyId") REFERENCES public."Parties"("Id") ON DELETE CASCADE;


--
-- Name: Party_Invites FK_Party_Invites_Users_InvitedByUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Invites"
    ADD CONSTRAINT "FK_Party_Invites_Users_InvitedByUserId" FOREIGN KEY ("InvitedByUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Party_Invites FK_Party_Invites_Users_InvitedUserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Invites"
    ADD CONSTRAINT "FK_Party_Invites_Users_InvitedUserId" FOREIGN KEY ("InvitedUserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Party_Members FK_Party_Members_Parties_PartyId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Members"
    ADD CONSTRAINT "FK_Party_Members_Parties_PartyId" FOREIGN KEY ("PartyId") REFERENCES public."Parties"("Id") ON DELETE CASCADE;


--
-- Name: Party_Members FK_Party_Members_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."Party_Members"
    ADD CONSTRAINT "FK_Party_Members_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: RefreshTokens FK_RefreshTokens_Users_UserId; Type: FK CONSTRAINT; Schema: public; Owner: -
--

ALTER TABLE ONLY public."RefreshTokens"
    ADD CONSTRAINT "FK_RefreshTokens_Users_UserId" FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- PostgreSQL database dump complete
--


