create table public.observation
(
    internal_id           bigserial constraint observation_pk key,
    id                    uuid                     not null,
    source                varchar(20)              not null,
    type                  varchar(20)              not null,
    valid_time_start      timestamp with time zone not null,
    valid_time_end        timestamp with time zone,
    phenomenon_time_start timestamp with time zone not null,
    phenomenon_time_end   timestamp with time zone,
    organisation          varchar(50)              not null,
    result_time           timestamp with time zone not null,
    modified_on           timestamp with time zone,
    result                jsonb                    not null,
    parameter             hstore                   not null,
    metadata              jsonb,
    is_public             boolean,
    sync                  timestamp with time zone,
    access                jsonb,
    classification        integer,
    geography             geometry,
    foi                   jsonb,
    foi_code              varchar
) with (autovacuum_enabled = true);

alter table public.observation    owner to postgres;
create index idx_gin_foi                 on public.observation using gin (foi);
create index idx_gin_metadata            on public.observation using gin (metadata);
create index idx_gin_parameter           on public.observation using gin (parameter);
create index idx_internal_id             on public.observation (internal_id);
create index idx_modified_on             on public.observation (modified_on);
create index idx_observation_foi_code    on public.observation (foi_code);
