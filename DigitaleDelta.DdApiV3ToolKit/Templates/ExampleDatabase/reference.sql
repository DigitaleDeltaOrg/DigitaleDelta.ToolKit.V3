create table public.reference
(
    internal_id   bigserial        constraint reference_pk            primary key,
    source        varchar(20)              not null,
    id            uuid                     not null,
    type          varchar(50)              not null,
    code          varchar(250)             not null,
    organisation  varchar(50)              not null,
    description   varchar(1000)            not null,
    sync          timestamp with time zone not null,
    modified_on   timestamp with time zone,
    geography     geometry,
    version       varchar(10),
    details       jsonb
);

alter table public.reference    owner to postgres;
create index id_idx            on public.reference (id);
create index idx_geography     on public.reference using gist (geography);
create index idx_type          on public.reference (type);
